﻿using System;
using System.Collections.Concurrent;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Exceptionless.Utility {
    public class SingleGlobalInstance : IDisposable {
        private readonly bool _hasHandle = false;
        private WaitHandle _waitHandle;
        private readonly string _key;
        private static readonly ConcurrentDictionary<string, WaitHandle> _namedLocks = new ConcurrentDictionary<string, WaitHandle>();

        private void InitWaitHandle() {
            string mutexId = String.Format("Global\\{{{0}}}", _key);

            try {
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);

                bool wasCreated = false;
#if NETSTANDARD1_5
                _waitHandle = new Mutex(false, mutexId, out wasCreated);
                ((Mutex)_waitHandle).SetAccessControl(securitySettings);
#else
                _waitHandle = new Mutex(false, mutexId, out wasCreated, securitySettings);
#endif
            } catch (Exception) {
                // We fallback to AutoResetEvent because Mutex isn't supported in medium trust.
                _waitHandle = _namedLocks.GetOrAdd(_key, key => new AutoResetEvent(true));
            }
        }

        public SingleGlobalInstance(string key, int millisecondsTimeout = Timeout.Infinite) {
            _key = key;
            InitWaitHandle();

            try {
#if NETSTANDARD1_5
                _hasHandle = _waitHandle.WaitOne(millisecondsTimeout);
#else
                _hasHandle = _waitHandle.WaitOne(millisecondsTimeout, false);
#endif

                if (_hasHandle == false)
                    throw new TimeoutException("Timeout waiting for exclusive access on SingleGlobalInstance");
            } catch (AbandonedMutexException) {
                _hasHandle = true;
            }
        }

        public bool HasHandle { get { return _hasHandle; } }

        public void Dispose() {
            if (_waitHandle == null)
                return;

            if (_hasHandle && _waitHandle is Mutex) {
                ((Mutex)_waitHandle).ReleaseMutex();
#if NETSTANDARD1_5
                _waitHandle.Dispose();
#else
                _waitHandle.Close();
#endif
            }

            if (_hasHandle && _waitHandle is AutoResetEvent)
                ((AutoResetEvent)_waitHandle).Set();

            _waitHandle = null;
        }
    }
}