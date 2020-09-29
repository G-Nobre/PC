using System.Collections.Generic;
using System.Threading;
using Utils;

namespace Ex4 {
    public class TransferQueue<E> {
        private readonly object _monitor = new object();
        private readonly LinkedList<SendRequest<E>> _sendRequestQueue = new LinkedList<SendRequest<E>>();
        private readonly LinkedList<Request<E>> _requestQueue = new LinkedList<Request<E>>();

        private readonly bool BLOCKING = true;

        public void Put(E message) {
            lock (_monitor) {
                if (_requestQueue.Count == 0) {
                    _sendRequestQueue.AddLast(new SendRequest<E>(message, !BLOCKING));
                }
                else {
                    Request<E> request = _requestQueue.First.Value;
                    _requestQueue.RemoveFirst();
                    request.Message = message;
                    request.Done = true;
                    Monitor.Pulse(_monitor);
                }
            }
        }

        public bool Transfer(E message, int timeout) {
            lock (_monitor) {
                if (_requestQueue.Count != 0) {
                    Request<E> request = _requestQueue.First.Value;
                    _requestQueue.RemoveFirst();
                    request.Message = message;
                    request.Done = true;
                    Monitor.Pulse(_monitor);
                }
                else {
                    SendRequest<E> request = new SendRequest<E>(message, BLOCKING);
                    _sendRequestQueue.AddLast(request);
                    TimeoutHolder timeoutHolder = new TimeoutHolder(timeout);
                    do {
                        try {
                            if (timeoutHolder.Value <= 0L) {
                                _sendRequestQueue.Remove(request);
                                return false;
                            }

                            Monitor.Wait(_monitor, timeout);
                        }
                        catch (ThreadInterruptedException ie) {
                            if (request.Done) {
                                Thread.CurrentThread.Interrupt();
                                break;
                                //will return true after the interruption, because the operation succeeded
                            }

                            _sendRequestQueue.Remove(request);
                            throw ie;
                        }
                    } while (_requestQueue.Count == 0);
                }

                return true;
            }
        }

        public E Take(int timeout) {
            lock (_monitor) {
                if (_sendRequestQueue.Count != 0) {
                    SendRequest<E> sendRequest = _sendRequestQueue.First.Value;
                    _sendRequestQueue.RemoveFirst();
                    if (sendRequest.IsBlocking) {
                        sendRequest.Done = true;
                        Monitor.Pulse(_monitor);
                    }

                    return sendRequest.Message;
                }

                Request<E> request = new Request<E>();
                _requestQueue.AddLast(request);

                TimeoutHolder timeoutHolder = new TimeoutHolder(timeout);
                do {
                    try {
                        if (timeoutHolder.Value <= 0L) {
                            _requestQueue.Remove(request);
                            return default;
                        }
    
                        Monitor.Wait(_monitor, timeout);
                    }
                    catch (ThreadInterruptedException ie) {
                        if (request.Done) {
                            Thread.CurrentThread.Interrupt();
                            break;
                            // will return message after break;
                        }

                        _requestQueue.Remove(request);
                        throw;
                    }
                } while (request.IsBusy);

                return request.Message;
            }
        }
    }
}