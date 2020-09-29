using System.Collections.Generic;
using System.Threading;
using Utils;

namespace Ex2 {
    public class BroadcastBox<E> {
        private readonly object _monitor = new object();
        private readonly LinkedList<RequestMessage<E>> waitingQueue = new LinkedList<RequestMessage<E>>();

        public int DeliverToAll(E message) {
            lock (_monitor) {
                if (waitingQueue.Count == 0) return 0;

                var affected = 0;

                foreach (var requestMessage in waitingQueue) {
                    requestMessage.Message = message;
                    requestMessage.Busy = false;
                    ++affected;
                }

                Monitor.PulseAll(_monitor);
                waitingQueue.Clear();
                return affected;
            }
        }

        public Optional<E> receive(int timeout) {
            lock (_monitor) {
                var request = new RequestMessage<E>(true);
                waitingQueue.AddFirst(request);
                var timeoutHolder = new TimeoutHolder(timeout);
                do {
                    try {
                        if ((timeout = timeoutHolder.Value) <= 0) {
                            waitingQueue.Remove(request);
                            return Optional<E>.Empty();
                        }

                        Monitor.Wait(_monitor, timeout);
                    }
                    catch (ThreadInterruptedException tie) {
                        if (!request.Busy) {
                            waitingQueue.Remove(request);
                            break;
                        }

                        throw;
                    }
                } while (request.Busy);

                return Optional<E>.Of(request.Message);
            }
        }
    }
}