using System.Collections.Generic;
using System.Threading;

namespace ConsoleApplication1 {
    public class Logger<T> {
        private class AllRequests {
            internal List<T> requests;
            internal bool done;
        }

        private readonly object _lock = new object();

        private readonly LinkedList<AllRequests> _requests = new LinkedList<AllRequests>();
        private List<T> messages = new List<T>();

        public List<T> TakeAll() {
            lock (_lock) {
                if (messages.Count > 0) {
                    List<T> ret = messages;
                    messages.Clear();
                    return ret;
                }

                LinkedListNode<AllRequests> listNode = _requests.AddFirst(new AllRequests());

                do {
                    try {
                        Monitor.Wait(_lock);
                    }
                    catch (ThreadInterruptedException) {
                        if (listNode.Value.done) {
                            Thread.CurrentThread.Interrupt();
                            break;
                        }

                        _requests.Remove(listNode);
                    }
                } while (!listNode.Value.done);

                return listNode.Value.requests;
            }
        }    

        public void Put(T message) {
            lock (_lock) {
                if (_requests.Count > 0) {
                    LinkedListNode<AllRequests> listNode = _requests.First;
                    _requests.RemoveFirst();

                    listNode.Value.done = true;
                    listNode.Value.requests = new List<T>();
                    listNode.Value.requests.Add(message);
                    
                    Monitor.Pulse(_lock);
                    return;
                }
                messages.Add(message);
            }
        }
    }
}