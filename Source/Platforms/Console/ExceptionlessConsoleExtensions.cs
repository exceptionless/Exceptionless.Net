#region Copyright 2014 Exceptionless

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
//     http://www.apache.org/licenses/LICENSE-2.0

#endregion

using System;
using Exceptionless.Dependency;
using Exceptionless.Logging;

namespace Exceptionless {
    public static class ExceptionlessConsoleExtensions {
        private static EventHandler _onProcessExit;

        public static void Register(this ExceptionlessClient client) {
            client.Startup();

            // make sure that queued events are sent when the app exits
            client.RegisterOnProcessExitHandler();
        }

        public static void Unregister(this ExceptionlessClient client) {
            client.Shutdown();
            client.UnregisterOnProcessExitHandler();
        }

        private static void RegisterOnProcessExitHandler(this ExceptionlessClient client) {
            if (_onProcessExit == null)
                _onProcessExit = (sender, args) => client.ProcessQueue();

            try {
                AppDomain.CurrentDomain.ProcessExit -= _onProcessExit;
                AppDomain.CurrentDomain.ProcessExit += _onProcessExit;
            } catch (Exception ex) {
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessConsoleExtensions), ex, "An error occurred while wiring up to the process exit event.");
            }
        }

        private static void UnregisterOnProcessExitHandler(this ExceptionlessClient client) {
            if (_onProcessExit == null)
                return;

            AppDomain.CurrentDomain.ProcessExit -= _onProcessExit;
            _onProcessExit = null;
        }
    }
}