﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless;
using Exceptionless.DateTimeExtensions;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Helpers;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.SampleConsole.Plugins;
using log4net;
using log4net.Config;
using NLog;
using LogLevel = Exceptionless.Logging.LogLevel;

namespace SampleConsole {
    internal class Program {
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

        private static void Main() {
            Console.CursorVisible = false;
            StartDisplayingLogMessages();

            ExceptionlessClient.Default.Configuration.UpdateSettingsWhenIdleInterval = TimeSpan.FromSeconds(15);
            ExceptionlessClient.Default.Configuration.UseTraceLogEntriesPlugin();
            ExceptionlessClient.Default.Configuration.AddPlugin<SystemUptimePlugin>();
            ExceptionlessClient.Default.Configuration.UseFolderStorage("store");
            ExceptionlessClient.Default.Configuration.UseLogger(_log);
            //ExceptionlessClient.Default.Configuration.SubmissionBatchSize = 1;
            ExceptionlessClient.Default.Register();

            // test NLog
            GlobalDiagnosticsContext.Set("GlobalProp", "GlobalValue");
            //Log.Info().Message("Hi").Tag("Tag1", "Tag2").Property("LocalProp", "LocalValue").MarkUnhandled("SomeMethod").ContextProperty("Blah", new Event()).Write();

            //ExceptionlessClient.Default.SubmitLog(typeof(Program).Name, "Trace Message", LogLevel.Trace);

            // test log4net
            XmlConfigurator.Configure();
            GlobalContext.Properties["GlobalProp"] = "GlobalValue";
            ThreadContext.Properties["LocalProp"] = "LocalValue";
            //_log4net.Info("Hi");

            var tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            
            ExceptionlessClient.Default.Configuration.AddPlugin(ctx => ctx.Event.Data[RandomData.GetWord()] = RandomData.GetWord());
            ExceptionlessClient.Default.Configuration.AddPlugin(ctx => ctx.Event.Data[RandomData.GetWord()] = RandomData.GetWord());
            ExceptionlessClient.Default.Configuration.AddPlugin(ctx => {
                // use server settings to see if we should include this data
                if (ctx.Client.Configuration.Settings.GetBoolean("IncludeConditionalData", true))
                    ctx.Event.AddObject(new { Total = 32.34, ItemCount = 2, Email = "someone@somewhere.com" }, "ConditionalData");
            });
            ExceptionlessClient.Default.Configuration.Settings.Changed += (sender, args) => Trace.WriteLine($"Action: {args.Action} Key: {args.Item.Key} Value: {args.Item.Value}");

            WriteOptionsMenu();

            while (true) {
                Console.SetCursorPosition(0, OPTIONS_MENU_LINE_COUNT + 1);
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

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
                throw new ApplicationException("Test");
            } catch (Exception ex){
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
            Task.Factory.StartNew(() => {
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
                    Thread.Sleep(250);
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

            string path = Path.GetFullPath(@"..\..\Errors\");
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