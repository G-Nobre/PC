using System;
using System.Runtime.InteropServices;
using System.Threading;
using Utils;

namespace Ex3 {
    public class Exchanger<T> {
        private readonly object _lock = new object();
        private Waiter<T> _waiter;

        public Optional<T> Exchange(T mydata, int timeout) {
            lock (_lock) {
                if (_waiter != null) {
                    Console.WriteLine("Swapee value = " + _waiter.MyData + ", swaper value = " + mydata);
                    mydata = _waiter.Swap(mydata);
                    _waiter.IsDone = true;
                    Monitor.Pulse(_waiter.Lock);
                    return Optional<T>.Of(mydata);
                }

                TimeoutHolder timeoutHolder = new TimeoutHolder(timeout);
                _waiter = new Waiter<T>(mydata, _lock);

                do {
                    try {
                        Console.WriteLine(_waiter.MyData);
                        if ((timeout = timeoutHolder.Value) <= 0) {
                            return Optional<T>.Empty();
                        }

                        Monitor.Wait(_waiter.Lock, timeout);
                    }
                    catch (ThreadInterruptedException e) {
                        if (_waiter.IsDone) {
                            Console.WriteLine("Done = true, value = " + _waiter.MyData);
                            Thread.CurrentThread.Interrupt();
                            break;
                        }

                        throw;
                    }
                } while (!_waiter.IsDone);

                Console.WriteLine("Will return value = " + _waiter.MyData);
                return Optional<T>.Of(_waiter.MyData);
            }
        }
    }
}