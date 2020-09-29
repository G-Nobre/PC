using System;
using System.Threading;
using Utils;

namespace Ex1 {
    public class BoundedLazy<E> {
        private readonly int _lives;
        private readonly object _monitor;
        private readonly Func<E> _supplier;

        private int _currentLives;
        private Exception _exceptionOccurred;
        private State _state;
        private E _value;


        public BoundedLazy(Func<E> supplier, int lives) {
            _monitor = new object();
            _supplier = supplier;

            if (lives < 0) throw new ArgumentException();

            _lives = lives;
            _currentLives = _lives;
            _state = State.Uncomputed;
        }

        private Optional<E> DecrementCheckAndReturn() {
            if (--_currentLives == 0)
                _state = State.Uncomputed;
            return Optional<E>.Of(_value);
        }

        public Optional<E> Get(int timeout) {
            lock (_monitor) {
                /*
                 * If exception has occurred, then every calls to get result
                 * in that same exception getting thrown again
                 */
                if (_state == State.Error) throw _exceptionOccurred;

                /*
                 * If the value is already computed and the number of lives is greater than 0,
                 * then the value is returned to caller right away.
                 */
                if (_currentLives > 0 && _state == State.Computed)
                    return DecrementCheckAndReturn();


                /*
                  In any other case, either the value needs to be recalculated, or is already being
                  computed by another thread. The current thread calculate the value, or wait,
                  if the latter is the case.
                 */
                if (_state == State.Computing) {
                    var timeoutHolder = new TimeoutHolder(timeout);
                    do {
                        if ((timeout = timeoutHolder.Value) < 0L) return Optional<E>.Empty();

                        Monitor.Wait(_monitor, timeout);

                        if (_state == State.Computed && _currentLives > 0) return DecrementCheckAndReturn();

                        if (_state == State.Error) throw _exceptionOccurred;
                    } while (_state == State.Computing);
                }

                _state = State.Computing;
            }

            /*
              The current thread is going to calculate the new value, by invoking the supplier.
             */
            E v = default;
            Exception ex = default;
            try {
                v = _supplier();
                _currentLives = _lives;
            }
            catch (Exception e) {
                ex = e;
            }

            /*
               In the end, if an unchecked exception has occurred, it shall be thrown, and State state
               shall be put to ERROR. Else, the value field is updated and returned.
             */
            lock (_monitor) {
                Monitor.PulseAll(_monitor);
                if (ex != null) {
                    _exceptionOccurred = ex;
                    _state = State.Error;
                    throw _exceptionOccurred;
                }

                _value = v;
                _state = State.Computed;
                return DecrementCheckAndReturn();
            }
        }
    }
}