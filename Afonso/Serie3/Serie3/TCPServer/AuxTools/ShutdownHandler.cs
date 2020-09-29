using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication1 {
    public class ShutdownHandler {
    
        private class ShutdownAsyncRequest<Response> : TaskCompletionSource<Response> {
            internal Timer Timer;
            const int Pending = 0, Locked = 1;
            private volatile int @lock;

            internal ShutdownAsyncRequest() {
                @lock = Pending;
            }

            internal void Dispose() {
                Timer?.Dispose();
            }

            internal bool TryLock() {
                return @lock == Pending &&
                       Interlocked.CompareExchange(ref @lock, Locked, Pending) == Pending;
            }

        }
        
        
        // private readonly LinkedList<AsyncRequest<Response>> queue = new LinkedList<AsyncRequest<Response>>();
        private ShutdownAsyncRequest<Response> request = null;
        private readonly HttpHelper _httpHelper;
        

        private readonly TimerCallback shutdownTimeoutHandler;
        private readonly object @lock = new object();

        public ShutdownHandler(HttpHelper httpHelper) {
            _httpHelper = httpHelper;
            shutdownTimeoutHandler = ShutdownTimeoutHandler;
        }

        private void ShutdownTimeoutHandler(object state) {
            LinkedListNode<ShutdownAsyncRequest<Response>> listNode = (LinkedListNode<ShutdownAsyncRequest<Response>>)state;
            ShutdownAsyncRequest<Response> shutdownAsyncRequest = listNode.Value;

            if (shutdownAsyncRequest != null && shutdownAsyncRequest.TryLock()) {
                lock (@lock) {
                    if (listNode.List != null)
                        request = null;
                }
                
                shutdownAsyncRequest.Dispose();
                shutdownAsyncRequest.SetResult(_httpHelper.ServerShutdownTimeout);
            }
        }

        public Task<Response> AwaitShutdown(CancellationTokenSource cancellationToken, int timeout) {
            lock (@lock) {
                cancellationToken.Cancel();

                if (timeout == 0) {
                    return Task.FromResult(_httpHelper.ServerShutdownTimeout);
                }
                
                request = new ShutdownAsyncRequest<Response>();
                var listNode = new LinkedListNode<ShutdownAsyncRequest<Response>>(request);
                
                if(timeout != Timeout.Infinite)
                    request.Timer = new Timer(shutdownTimeoutHandler,listNode, timeout,Timeout.Infinite);

                return request.Task;
            }
        }

        public void NotifyShutdownCompleted() {
            request?.SetResult(_httpHelper.OperationSuccessful());
        }        
    }
}