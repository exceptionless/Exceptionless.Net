using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Storage;
using Exceptionless.Submission;

namespace Exceptionless.Queue {
    public class DefaultEventQueue : IEventQueue {
        private readonly IExceptionlessLog _log;
        private readonly ExceptionlessConfiguration _config;
        private readonly ISubmissionClient _client;
        private readonly IObjectStorage _storage;
        private readonly IJsonSerializer _serializer;
        private Timer _queueTimer;
        private Task _processingQueueTask;
        private readonly object _sync = new object();
        private readonly TimeSpan _processQueueInterval = TimeSpan.FromSeconds(10);
        private DateTime? _suspendProcessingUntil;
        private DateTime? _discardQueuedItemsUntil;

        public DefaultEventQueue(ExceptionlessConfiguration config, IExceptionlessLog log, ISubmissionClient client, IObjectStorage objectStorage, IJsonSerializer serializer): this(config, log, client, objectStorage, serializer, null, null) {}

        public DefaultEventQueue(ExceptionlessConfiguration config, IExceptionlessLog log, ISubmissionClient client, IObjectStorage objectStorage, IJsonSerializer serializer, TimeSpan? processQueueInterval, TimeSpan? queueStartDelay) {
            _log = log;
            _config = config;
            _client = client;
            _storage = objectStorage;
            _serializer = serializer;
            if (processQueueInterval.HasValue)
                _processQueueInterval = processQueueInterval.Value;

            _queueTimer = new Timer(OnProcessQueue, null, queueStartDelay ?? TimeSpan.FromSeconds(2), _processQueueInterval);
        }

        public void Enqueue(Event ev) {
            if (AreQueuedItemsDiscarded) {
                _log.Info(typeof(ExceptionlessClient), "Queue items are currently being discarded. The event will not be queued.");
                return;
            }

            _storage.Enqueue(_config.GetQueueName(), ev);
        }

        public Task ProcessAsync() {
            return Task.Run(Process);
        }

        public Task Process() {
            if (!_config.Enabled) {
                _log.Info(typeof(DefaultEventQueue), "Configuration is disabled. The queue will not be processed.");
                return Task.FromResult(false);
            }

            TaskCompletionSource<bool> tcs;
            lock (_sync) {
                if (_processingQueueTask != null) {
                    return _processingQueueTask;
                } else {
                    tcs = new TaskCompletionSource<bool>();
                    _processingQueueTask = tcs.Task;
                }
            }

            Task resultTask;
            try {
                _log.Trace(typeof(DefaultEventQueue), "Processing queue...");
                string queueName = _config.GetQueueName();
                _storage.CleanupQueueFiles(queueName, _config.QueueMaxAge, _config.QueueMaxAttempts);
                _storage.ReleaseStaleLocks(queueName);

                DateTime maxCreatedDate = DateTime.Now;
                int batchSize = _config.SubmissionBatchSize;
                var batch = _storage.GetEventBatch(queueName, _serializer, batchSize, maxCreatedDate);
                while (batch.Any()) {
                    bool deleteBatch = true;

                    try {
                        var events = batch.Select(b => b.Item2).ToList();
                        var response = _client.PostEvents(events, _config, _serializer);
                        if (response.Success) {
                            _log.FormattedInfo(typeof(DefaultEventQueue), "Sent {0} events to \"{1}\".", batch.Count, _config.ServerUrl);
                        } else if (response.ServiceUnavailable) {
                            // You are currently over your rate limit or the servers are under stress.
                            _log.Error(typeof(DefaultEventQueue), "Server returned service unavailable.");
                            SuspendProcessing();
                            deleteBatch = false;
                        } else if (response.PaymentRequired) {
                            // If the organization over the rate limit then discard the event.
                            _log.Warn(typeof(DefaultEventQueue), "Too many events have been submitted, please upgrade your plan.");
                            SuspendProcessing(discardFutureQueuedItems: true, clearQueue: true);
                        } else if (response.UnableToAuthenticate) {
                            // The api key was suspended or could not be authorized.
                            _log.Error(typeof(DefaultEventQueue), "Unable to authenticate, please check your configuration. The event will not be submitted.");
                            SuspendProcessing(TimeSpan.FromMinutes(15));
                        } else if (response.NotFound || response.BadRequest) {
                            // The service end point could not be found.
                            _log.FormattedError(typeof(DefaultEventQueue), "Error while trying to submit data: {0}", response.Message);
                            SuspendProcessing(TimeSpan.FromHours(4));
                        } else if (response.RequestEntityTooLarge) {
                            if (batchSize > 1) {
                                _log.Error(typeof(DefaultEventQueue), "Event submission discarded for being too large. The event will be retried with a smaller batch size.");
                                batchSize = Math.Max(1, (int)Math.Round(batchSize / 1.5d, 0));
                                deleteBatch = false;
                            } else {
                                _log.Error(typeof(DefaultEventQueue), "Event submission discarded for being too large. The event will not be submitted.");
                            }
                        } else if (!response.Success) {
                            _log.Error(typeof(DefaultEventQueue), response.Exception, 
                                String.IsNullOrEmpty(response.Message) ? "An error occurred while submitting events." : 
                                String.Concat("An error occurred while submitting events: ", response.Message));
                            SuspendProcessing();
                            deleteBatch = false;
                        }

                        OnEventsPosted(new EventsPostedEventArgs { Events = events, Response = response });
                    } catch (AggregateException ex) {
                        _log.Error(typeof(DefaultEventQueue), ex, String.Concat("An error occurred while submitting events: ", ex.Flatten().Message));
                        SuspendProcessing();
                        deleteBatch = false;
                    } catch (Exception ex) {
                        _log.Error(typeof(DefaultEventQueue), ex, String.Concat("An error occurred while submitting events: ", ex.Message));
                        SuspendProcessing();
                        deleteBatch = false;
                    }

                    if (deleteBatch)
                        _storage.DeleteBatch(batch);
                    else
                        _storage.ReleaseBatch(batch);

                    if (!deleteBatch || IsQueueProcessingSuspended)
                        break;

                    batch = _storage.GetEventBatch(queueName, _serializer, batchSize, maxCreatedDate);
                }
            } catch (Exception ex) {
                _log.Error(typeof(DefaultEventQueue), ex, String.Concat("An error occurred while processing the queue: ", ex.Message));
                SuspendProcessing();
            } finally {
                tcs.SetResult(true);
                lock (_sync) {
                    _processingQueueTask = null;
                    resultTask = tcs.Task;
                }
            }
            return resultTask;
        }

        private void OnProcessQueue(object state) {
            if (IsQueueProcessingSuspended)
                return;

            Process();
        }

        public void SuspendProcessing(TimeSpan? duration = null, bool discardFutureQueuedItems = false, bool clearQueue = false) {
            if (!duration.HasValue)
                duration = TimeSpan.FromMinutes(5);

            _log.Info(typeof(DefaultEventQueue), String.Format("Suspending processing for: {0}.", duration.Value));
            _suspendProcessingUntil = DateTime.Now.Add(duration.Value);
            _queueTimer.Change(duration.Value, _processQueueInterval);

            if (discardFutureQueuedItems)
                _discardQueuedItemsUntil = DateTime.Now.Add(duration.Value);

            if (!clearQueue)
                return;

            // Account is over the limit and we want to ensure that the sample size being sent in will contain newer errors.
            try {
#pragma warning disable 4014
                _storage.CleanupQueueFiles(_config.GetQueueName(), TimeSpan.Zero);
#pragma warning restore 4014
            } catch (Exception) { }
        }

        public event EventHandler<EventsPostedEventArgs> EventsPosted;

        protected virtual void OnEventsPosted(EventsPostedEventArgs e) {
            try {
                if (EventsPosted != null)
                    EventsPosted.Invoke(this, e);
            } catch (Exception ex) {
                _log.Error(typeof(DefaultEventQueue), ex, "Error while calling OnEventsPosted event handlers.");
            }
        }

        internal bool IsQueueProcessingSuspended {
            get { return _suspendProcessingUntil.HasValue && _suspendProcessingUntil.Value > DateTime.Now; }
        }

        internal bool AreQueuedItemsDiscarded {
            get { return _discardQueuedItemsUntil.HasValue && _discardQueuedItemsUntil.Value > DateTime.Now; }
        }

        public void Dispose() {
            if (_queueTimer == null)
                return;

            _queueTimer.Dispose();
            _queueTimer = null;
        }
    }
}