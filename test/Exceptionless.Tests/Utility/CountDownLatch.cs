using System;
using System.Threading;

namespace Exceptionless.Tests.Utility {
    public class CountDownLatch {
        private int _remaining;
        private ManualResetEventSlim _event;

        public CountDownLatch(int count) {
            Reset(count);
        }

        public void Reset(int count) {
            if (count < 0)
                throw new ArgumentOutOfRangeException();
            _remaining = count;
            _event = new ManualResetEventSlim(false);
            if (_remaining == 0)
                _event.Set();
        }

        public void Signal() {
            // The last thread to signal also sets the event.
            if (Interlocked.Decrement(ref _remaining) == 0)
                _event.Set();
        }

        public bool Wait(int millisecondsTimeout) {
            return _event.Wait(millisecondsTimeout);
        }

        public int Remaining { get { return _remaining; } }
    }
}
