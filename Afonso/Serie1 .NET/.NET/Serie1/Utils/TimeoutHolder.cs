using System;
using System.Threading;

namespace Utils {
    public struct TimeoutHolder {
        private int _timeout;
        private int _refTime;

        public TimeoutHolder(int timeout) {
            _timeout = timeout;
            _refTime = (timeout != 0 && timeout != Timeout.Infinite) ? Environment.TickCount : 0;
        }

        // returns the remaining timeout
        public int Value {
            get {
                if (_timeout != 0 && _timeout != Timeout.Infinite) {
                    int now = Environment.TickCount;
                    if (now != _refTime) {
                        int elapsed = now - _refTime;
                        _refTime = now;
                        _timeout = elapsed < _timeout ? _timeout - elapsed : 0;
                    }
                }

                return _timeout;
            }
        }
    }
}