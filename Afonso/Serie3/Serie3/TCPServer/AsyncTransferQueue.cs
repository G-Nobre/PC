using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication1 {
    public class AsyncTransferQueue<T> {
        internal class AsyncRequest<TV> : TaskCompletionSource<TV> {
            internal readonly CancellationToken _cToken;
            internal CancellationTokenRegistration _cTokenRegistration;
            internal Timer _timer;
            const int Pending = 0, Locked = 1;
            internal volatile int _lock;

            internal AsyncRequest(CancellationToken cToken) {
                _cToken = cToken;
                _lock = Pending;
            }

            internal bool TryLock() {
                return _lock == Pending &&
                       Interlocked.CompareExchange(ref _lock, Locked, Pending) == Pending;
            }

            internal void Dispose(bool canceling = false) {
                if (!canceling && _cToken.CanBeCanceled) {
                    _cTokenRegistration.Dispose();
                }

                _timer?.Dispose();
            }
        }

        // Type used by async take requests
        private class AsyncTake : AsyncRequest<T> {
            internal AsyncTake(CancellationToken cToken) : base(cToken) { }
        }

        // Type used by async transfer take request
        internal class AsyncTransfer : AsyncRequest<bool> {
            internal AsyncTransfer(CancellationToken cToken) : base(cToken) { }
        }

        // Type used to hold each message sent with put or with transfer 
        public class Message {
            internal readonly T message;

            // This field holds the reference to the AsyncTransfer when the
            // when the underlying message was sent with transfer, null otherwise.		
            internal readonly AsyncTransfer transfer;

            internal Message(T message, AsyncTransfer transfer = null) {
                this.message = message;
                this.transfer = transfer;
            }
        }

        // Queue of messages pending for reception
        private readonly LinkedList<Message> _pendingMessage = new LinkedList<Message>();

        // Queue of pending async take requests
        private readonly LinkedList<AsyncTake> _asyncTakes = new LinkedList<AsyncTake>();


        private readonly object _lock = new object();

        private readonly Action<object> _takeCancellationHandler;
        private readonly TimerCallback _takeTimeoutHandler;

        private readonly Action<object> _transferCancellationHandler;
        private readonly TimerCallback _transferTimeoutHandler;

        /**
	    *  Completed tasks use to return constant results from the AcquireAsync method
	    */
        private static readonly Task<bool> trueTask = Task.FromResult<bool>(true);

        private static readonly Task<bool> falseTask = Task.FromResult<bool>(false);

        private static readonly Task<bool> argExceptionTask =
            Task.FromException<bool>(new ArgumentException("acquires"));

        public AsyncTransferQueue() {
            _takeCancellationHandler = request => TakeCancellationHandler(request, true);
            _transferCancellationHandler = request => TransferCancelletionHandler(request, true);

            _takeTimeoutHandler = request => TakeCancellationHandler(request, false);
            _transferTimeoutHandler = request => TransferCancelletionHandler(request, false);
        }

        private void TakeCancellationHandler(object request, bool canceling) {
            LinkedListNode<AsyncTake> node = (LinkedListNode<AsyncTake>) request;
            AsyncTake take = node.Value;

            if (take.TryLock()) {
                lock (_lock) {
                    if (node.List != null) {
                        _asyncTakes.Remove(node);
                    }
                }

                take.Dispose();

                if (canceling) {
                    take.SetCanceled();
                }
                else {
                    take.SetResult(default);
                }
            }
        }

        private void TransferCancelletionHandler(object request, bool canceling) {
            LinkedListNode<Message> node = (LinkedListNode<Message>) request;
            AsyncTransfer transfer = node.Value.transfer;

            if (transfer.TryLock()) {
                lock (_lock) {
                    if (node.List != null) {
                        _pendingMessage.Remove(node);
                    }

                    transfer.Dispose();

                    if (canceling) {
                        transfer.SetCanceled();
                    }
                    else {
                        transfer.SetResult(default);
                    }
                }
            }
        }

        private AsyncTake SatisfyPendingAsyncTake() {
            AsyncTake take = null;

            while (_asyncTakes.Count > 0) {
                AsyncTake request = _asyncTakes.First.Value;

                _asyncTakes.RemoveFirst();

                if (request.TryLock()) {
                    take = request;
                    break;
                }
            }

            return take;
        }

        private Message SatisfyPendingAsyncTransfer() {
            Message message = null;

            while (_pendingMessage.Count > 0) {
                Message request = _pendingMessage.First.Value;
                
                _pendingMessage.RemoveFirst();

                if (request.transfer.TryLock()) {
                    message = request;
                    break;
                }
            }

            return message;
        }

        private void CompleteSatisfiedAsyncTake(AsyncTake satisfiedTake, T message) {
            if (satisfiedTake != null) {
                satisfiedTake.Dispose();
                satisfiedTake.SetResult(message);
            }
        }

        private void CompleteSatisfiedAsyncTransfer(AsyncTransfer satisfiedTransfer) {
            if (satisfiedTransfer != null) {
                satisfiedTransfer.Dispose();
                satisfiedTransfer.SetResult(true);
            }
        }

        public void PutAsync(T message) {
            AsyncTake satisfied;

            lock (_lock) {
                satisfied = SatisfyPendingAsyncTake();

                if (satisfied == null) {
                    AsyncTransfer put = new AsyncTransfer(default);
                    Message putMessage = new Message(message,put);

                    _pendingMessage.AddLast(putMessage);
                    return;
                }
            }
            
            CompleteSatisfiedAsyncTake(satisfied,message);
        }

        public Task<T> TakeAsync(int timeout = Timeout.Infinite, CancellationToken cToken = default) {
            Message message; 
            lock (_lock) {
                if (_pendingMessage.Count > 0) {
                    message = SatisfyPendingAsyncTransfer();
                }
                else {
                    if (timeout == 0)
                        return Task.FromException<T>(new TimeoutException("Invalid Timeout!"));
                    if (cToken.IsCancellationRequested)
                        return Task.FromCanceled<T>(cToken);
                    
                    AsyncTake take = new AsyncTake(cToken);
                    LinkedListNode<AsyncTake> requestNode = _asyncTakes.AddLast(take);
                    
                    take._timer = new Timer(_takeTimeoutHandler,requestNode,timeout,Timeout.Infinite);
                    if (cToken.CanBeCanceled) {
                        take._cTokenRegistration = cToken.Register(_takeCancellationHandler, requestNode);
                    }

                    return take.Task;
                }
                
            }
            CompleteSatisfiedAsyncTransfer(message.transfer);
            return Task.FromResult(message.message);
        }

        public Task<bool> TransferAsync(T message, int timeout = Timeout.Infinite, CancellationToken cToken = default) {
            AsyncTake request;

            lock (_lock) {
                if (_asyncTakes.Count > 0) {
                    request = SatisfyPendingAsyncTake();
                }
                else {
                    if (timeout == 0)
                        return falseTask;
                    if (cToken.IsCancellationRequested)
                        return Task.FromCanceled<bool>(cToken);

                    AsyncTransfer transfer = new AsyncTransfer(cToken);
                    Message messageRequest = SatisfyPendingAsyncTransfer();

                    LinkedListNode<Message> linkedListNode = _pendingMessage.AddLast(messageRequest);
                    
                    transfer._timer = new Timer(_transferTimeoutHandler,linkedListNode,timeout,Timeout.Infinite);
                    if (cToken.CanBeCanceled) {
                        transfer._cTokenRegistration = cToken.Register(_transferCancellationHandler, messageRequest);
                    }

                    return transfer.Task;
                }
            }
            CompleteSatisfiedAsyncTake(request,message);
            return trueTask;
        }
    }
}