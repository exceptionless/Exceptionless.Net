using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless.Configuration;
using Exceptionless.DateTimeExtensions;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Helpers;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.NLog;
using Exceptionless.SampleConsole.Plugins;
#if NET45
using log4net;
using log4net.Config;
#endif
using NLog;
using NLog.Config;
using NLog.Fluent;
using LogLevel = Exceptionless.Logging.LogLevel;

// example of setting an attribute value in config.
[assembly: Exceptionless("LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw", ServerUrl = "http://localhost:5000")] 
[assembly: ExceptionlessSetting("EnableWelcomeMessage", "True")]

namespace Exceptionless.SampleConsole {
    public class Program {
        private static readonly int[] _delays = { 0, 50, 100, 1000 };
        private static int _delayIndex = 2;
        private static readonly InMemoryExceptionlessLog _log = new InMemoryExceptionlessLog { MinimumLogLevel = LogLevel.Info };
        private static readonly object _writeLock = new object();

        private static readonly TimeSpan[] _dateSpans = {
            TimeSpan.Zero,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromHours(1),
            TimeSpan.FromDays(1),
            TimeSpan.FromDays(7),
            TimeSpan.FromDays(TimeSpanExtensions.AvgDaysInAMonth),
            TimeSpan.FromDays(TimeSpanExtensions.AvgDaysInAMonth * 3)
        };
        private static int _dateSpanIndex = 3;

        public static void Main(string[] args) {
            Console.CursorVisible = false;
            if (!Console.IsInputRedirected)
                StartDisplayingLogMessages();

            ExceptionlessClient.Default.Configuration.AddPlugin<SystemUptimePlugin>();
            ExceptionlessClient.Default.Configuration.UseFolderStorage("store");
            ExceptionlessClient.Default.Configuration.UseLogger(_log); 
            ExceptionlessClient.Default.Startup();

            if (ExceptionlessClient.Default.Configuration.Settings.GetBoolean("EnableWelcomeMessage", false))
                Console.WriteLine($"Hello {Environment.MachineName}!");

            // Test NLog
            var config = new LoggingConfiguration();
            var exceptionlessTarget = new ExceptionlessTarget();
            config.AddTarget("exceptionless", exceptionlessTarget);
            config.LoggingRules.Add(new LoggingRule("*", global::NLog.LogLevel.Debug, exceptionlessTarget));
            LogManager.Configuration = config;

            var logger = LogManager.GetCurrentClassLogger();
            logger.Warn()
                .Message("App Starting...")
                .Tag("Tag1", "Tag2")
                .Property("LocalProp", "LocalValue")
                .Property("Order", new { Total = 15 })
                .Write();

            // This is how you could log the same message using the fluent api directly.
            //ExceptionlessClient.Default.CreateLog(typeof(Program).Name, "App Starting...", LogLevel.Info)
            //    .AddTags("Tag1", "Tag2")
            //    .SetProperty("LocalProp", "LocalValue")
            //    .SetProperty("Order", new { Total = 15 })
            //    .Submit();
#if NET45
            // Test log4net
            XmlConfigurator.Configure();
            GlobalContext.Properties["GlobalProp"] = "GlobalValue";
            ThreadContext.Properties["LocalProp"] = "LocalValue";
            //_log4net.Info("Hi");
#endif

            var tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            ExceptionlessClient.Default.Configuration.AddPlugin(ctx => ctx.Event.Data[RandomData.GetWord()] = RandomData.GetWord());
            ExceptionlessClient.Default.Configuration.AddPlugin(ctx => {
                // use server settings to see if we should include this data
                if (ctx.Client.Configuration.Settings.GetBoolean("IncludeConditionalData", true))
                    ctx.Event.AddObject(new { Total = 32.34, ItemCount = 2, Email = "someone@somewhere.com" }, "ConditionalData");
            });
            ExceptionlessClient.Default.Configuration.Settings.Changed += (sender, changedArgs) => Trace.WriteLine($"Action: {changedArgs.Action} Key: {changedArgs.Item.Key} Value: {changedArgs.Item.Value}");

            WriteOptionsMenu();

            while (true) {
                Console.SetCursorPosition(0, OPTIONS_MENU_LINE_COUNT + 1);
                var keyInfo = Console.IsInputRedirected ? GetKeyFromRedirectedConsole() : Console.ReadKey(true);

                if (keyInfo.Key == ConsoleKey.D1)
                    SendEvent();
                else if (keyInfo.Key == ConsoleKey.D2)
                    SendContinuousEvents(50, token, 100);
                else if (keyInfo.Key == ConsoleKey.D3)
                    SendContinuousEvents(_delays[_delayIndex], token);
                else if (keyInfo.Key == ConsoleKey.D4) {
                    ExceptionlessClient.Default.SubmitSessionStart();
                } else if (keyInfo.Key == ConsoleKey.D5) {
                    ExceptionlessClient.Default.Configuration.UseSessions(false, null, true);
                    ExceptionlessClient.Default.SubmitSessionStart();
                } else if (keyInfo.Key == ConsoleKey.D6)
                    SendContinuousEvents(250, token, ev: new Event { Type = Event.KnownTypes.Log, Source = "SampleConsole.Program.Main", Message = "Sample console application event" });
                else if (keyInfo.Key == ConsoleKey.D7)
                    ExceptionlessClient.Default.SubmitSessionEnd();
                else if (keyInfo.Key == ConsoleKey.D8)
                    ExceptionlessClient.Default.Configuration.SetUserIdentity(Guid.NewGuid().ToString("N"));
                else if (keyInfo.Key == ConsoleKey.P) {
                    Console.SetCursorPosition(0, OPTIONS_MENU_LINE_COUNT + 2);
                    Console.WriteLine("Telling client to process the queue...");

                    ExceptionlessClient.Default.ProcessQueue();

                    ClearOutputLines();
                } else if (keyInfo.Key == ConsoleKey.F) {
                    SendAllCapturedEventsFromDisk();
                    ClearOutputLines();
                } else if (keyInfo.Key == ConsoleKey.D) {
                    _dateSpanIndex++;
                    if (_dateSpanIndex == _dateSpans.Length)
                        _dateSpanIndex = 0;
                    WriteOptionsMenu();
                } else if (keyInfo.Key == ConsoleKey.T) {
                    _delayIndex++;
                    if (_delayIndex == _delays.Length)
                        _delayIndex = 0;
                    WriteOptionsMenu();
                } else if (keyInfo.Key == ConsoleKey.Q)
                    break;
                else if (keyInfo.Key == ConsoleKey.S) {
                    tokenSource.Cancel();
                    tokenSource = new CancellationTokenSource();
                    token = tokenSource.Token;
                    ClearOutputLines();
                }
            }
        }

        private static ConsoleKeyInfo GetKeyFromRedirectedConsole() {
            string input = Console.In.ReadLine();
            switch (input?.ToLower()) {
                case "1":
                    return new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false);
                case "2":
                    return new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false);
                case "3":
                    return new ConsoleKeyInfo('3', ConsoleKey.D3, false, false, false);
                case "4":
                    return new ConsoleKeyInfo('4', ConsoleKey.D4, false, false, false);
                case "5":
                    return new ConsoleKeyInfo('5', ConsoleKey.D5, false, false, false);
                case "6":
                    return new ConsoleKeyInfo('6', ConsoleKey.D6, false, false, false);
                case "7":
                    return new ConsoleKeyInfo('7', ConsoleKey.D7, false, false, false);
                case "8":
                    return new ConsoleKeyInfo('8', ConsoleKey.D8, false, false, false);
                case "p":
                    return new ConsoleKeyInfo('p', ConsoleKey.P, false, false, false);
                case "f":
                    return new ConsoleKeyInfo('f', ConsoleKey.F, false, false, false);
                case "d":
                    return new ConsoleKeyInfo('d', ConsoleKey.D, false, false, false);
                case "t":
                    return new ConsoleKeyInfo('t', ConsoleKey.T, false, false, false);
                case "q":
                    return new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false);
            }

            return new ConsoleKeyInfo(' ', ConsoleKey.Escape, false, false, false);
        }

        private static void SampleApiUsages() {
            ExceptionlessClient.Default.CreateLog("SampleConsole", "Has lots of extended data")
                .AddObject(new { myApplicationVersion = new Version(1, 0), Date = DateTime.Now, __sessionId = "9C72E4E8-20A2-469B-AFB9-492B6E349B23", SomeField10 = "testing" }, "Object From Code")
                .AddObject(new { Blah = "Test" }, name: "Test Object")
                .AddObject("Exceptionless is awesome", "String Content")
                .AddObject(new int[] { 1, 2, 3, 4, 5 }, "Array Content")
                .AddObject(new object[] { new { This = "This" }, new { Is = "Is" }, new { A = "A" }, new { Test = "Test", Data = new { Punctuation = "!!!!" } } }, "Array With Nested Content")
                .Submit();

            ExceptionlessClient.Default.SubmitFeatureUsage("MyFeature");
            ExceptionlessClient.Default.SubmitNotFound("/somepage");
            ExceptionlessClient.Default.SubmitSessionStart();

            try {
                throw new Exception("Test");
            } catch (Exception ex) {
                ex.ToExceptionless().AddTags("SomeTag").Submit();
            }
        }

        private const int OPTIONS_MENU_LINE_COUNT = 15;
        private static void WriteOptionsMenu() {
            lock (_writeLock) {
                Console.SetCursorPosition(0, 0);
                ClearConsoleLines(0, OPTIONS_MENU_LINE_COUNT - 1);
                Console.WriteLine("1: Send 1");
                Console.WriteLine("2: Send 100");
                Console.WriteLine("3: Send continuous");
                Console.WriteLine("4: Send session start");
                Console.WriteLine("5: Send session start (manual)");
                Console.WriteLine("6: Send continuous log event");
                Console.WriteLine("7: Send session end");
                Console.WriteLine("8: Change user identity");
                Console.WriteLine("P: Process queue");
                Console.WriteLine("F: Process event files directory");
                Console.WriteLine("D: Change date range (" + _dateSpans[_dateSpanIndex].ToWords() + ")");
                Console.WriteLine("T: Change continuous delay (" + _delays[_delayIndex].ToString("N0") + ")");
                Console.WriteLine();
                Console.WriteLine("Q: Quit");
            }
        }

        private static void ClearOutputLines(int delay = 1000) {
            Task.Run(() => {
                Thread.Sleep(delay);
                ClearConsoleLines(OPTIONS_MENU_LINE_COUNT, OPTIONS_MENU_LINE_COUNT + 4);
            });
        }

        private const int LOG_LINE_COUNT = 10;
        private static void StartDisplayingLogMessages() {
            Task.Factory.StartNew(async () => {
                while (true) {
                    var logEntries = _log.GetLogEntries(LOG_LINE_COUNT);
                    lock (_writeLock) {
                        ClearConsoleLines(OPTIONS_MENU_LINE_COUNT + 5, OPTIONS_MENU_LINE_COUNT + 6 + LOG_LINE_COUNT);
                        Console.SetCursorPosition(0, OPTIONS_MENU_LINE_COUNT + 6);
                        foreach (var logEntry in logEntries) {
                            var originalColor = Console.ForegroundColor;
                            Console.ForegroundColor = GetColor(logEntry);
                            Console.WriteLine(logEntry);
                            Console.ForegroundColor = originalColor;
                        }
                    }

                    await Task.Delay(250);
                }
            });
        }

        private static ConsoleColor GetColor(InMemoryExceptionlessLog.LogEntry logEntry) {
            if (logEntry.Level == LogLevel.Debug)
                return ConsoleColor.Gray;
            if (logEntry.Level == LogLevel.Error)
                return ConsoleColor.Yellow;
            if (logEntry.Level == LogLevel.Info)
                return ConsoleColor.White;
            if (logEntry.Level == LogLevel.Trace)
                return ConsoleColor.DarkGray;
            if (logEntry.Level == LogLevel.Warn)
                return ConsoleColor.Magenta;

            return ConsoleColor.White;
        }

        private static void ClearConsoleLines(int startLine = 0, int endLine = -1) {
            if (endLine < 0)
                endLine = Console.WindowHeight - 2;

            lock (_writeLock) {
                int currentLine = Console.CursorTop;
                int currentPosition = Console.CursorLeft;

                for (int i = startLine; i <= endLine; i++) {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', Console.WindowWidth));
                }
                Console.SetCursorPosition(currentPosition, currentLine);
            }
        }

        private static void SendContinuousEvents(int delay, CancellationToken token, int maxEvents = Int32.MaxValue, int maxDaysOld = 90, Event ev = null) {
            Console.SetCursorPosition(0, OPTIONS_MENU_LINE_COUNT + 2);
            Console.WriteLine("Press 's' to stop sending.");
            int eventCount = 0;

            var levels = new[] { LogLevel.Trace, LogLevel.Debug, LogLevel.Info, LogLevel.Warn, LogLevel.Error, LogLevel.Fatal, LogLevel.Other };

            Task.Factory.StartNew(delegate {
                while (eventCount < maxEvents) {
                    if (token.IsCancellationRequested)
                        break;

                    if (ev != null && ev.IsLog())
                        ev.SetProperty(Event.KnownDataKeys.Level, levels.Random().Name);

                    SendEvent(ev, false);
                    eventCount++;
                    lock (_writeLock) {
                        Console.SetCursorPosition(0, OPTIONS_MENU_LINE_COUNT + 4);
                        Console.WriteLine("Submitted {0} events.", eventCount);
                    }

                    Thread.Sleep(delay);
                }

                ClearOutputLines();
            }, token);
        }

        private static readonly RandomEventGenerator _rnd = new RandomEventGenerator();
        private static void SendEvent(Event ev = null, bool writeToConsole = true) {
            _rnd.MinDate = DateTime.Now.Subtract(_dateSpans[_dateSpanIndex]);
            _rnd.MaxDate = DateTime.Now;

            ExceptionlessClient.Default.SubmitEvent(ev ?? _rnd.Generate());

            if (writeToConsole) {
                lock (_writeLock) {
                    Console.SetCursorPosition(0, OPTIONS_MENU_LINE_COUNT + 2);
                    Console.WriteLine("Submitted 1 event.");
                    Trace.WriteLine("Submitted 1 event.");
                }

                ClearOutputLines();
            }
        }

        private static void SendAllCapturedEventsFromDisk() {
            lock (_writeLock) {
                Console.SetCursorPosition(0, OPTIONS_MENU_LINE_COUNT + 2);
                Console.WriteLine("Sending captured events...");
            }

            string path = Path.GetFullPath(Path.Combine("..", "..", "Errors"));
            if (!Directory.Exists(path))
                return;

            int eventCount = 0;
            foreach (string file in Directory.GetFiles(path)) {
                var serializer = DependencyResolver.Default.GetJsonSerializer();
                var e = serializer.Deserialize<Event>(file);
                ExceptionlessClient.Default.SubmitEvent(e);

                eventCount++;
                lock (_writeLock) {
                    Console.SetCursorPosition(0, OPTIONS_MENU_LINE_COUNT + 3);
                    Console.WriteLine("Sent {0} events.", eventCount);
                }
            }
        }
    }
}