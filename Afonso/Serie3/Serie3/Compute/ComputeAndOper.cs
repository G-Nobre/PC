using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Compute {
    public class ComputeAndOper<T, R> : Exception where T : class {
        public static void Main(string[] args) { }

        public ComputeAndOper(string message = "communication error") : base(message) { }

        const int MIN_OPER_TIME = 100;
        const int MAX_OPER_TIME = 1000;


        static async Task<R> OperAsync(T argument, CancellationToken ctoken, Func<T, R> func) {
            var rnd = new Random(Environment.TickCount);
            try {
                await Task.Delay(rnd.Next(MIN_OPER_TIME, MAX_OPER_TIME), ctoken);
                if (rnd.Next(0, 100) >= 20)
                    throw new CommunicationException();
                return func(argument);
            }
            catch (OperationCanceledException) {
                Console.WriteLine("***delay canceled");
                throw;
            }
        }

        static async Task<R> OperRetryAsync(T argument, int maxRetries, CancellationTokenSource linkedCtokenSource, Func<T, R> func) {
            while (maxRetries > 0) {
                try {
                    return await OperAsync(argument, linkedCtokenSource.Token, func);
                }
                catch (CommunicationException) {
                    if (--maxRetries == 0) {
                        linkedCtokenSource.Cancel();
                        throw new MaxRetriesException<T>("Max retries attempted!",argument);
                    }
                }
            }
            return default;
        }

        public static async Task<R[]> ComputeAsync(T[] elems, int maxRetries, CancellationToken ctoken, Func<T, R> func) {
            CancellationTokenSource linkedCtokenSource = CancellationTokenSource.CreateLinkedTokenSource(ctoken);

            List<Task<R>> res = new List<Task<R>>();

            foreach (T elem in elems) {
                res.Add(OperRetryAsync(elem, maxRetries, linkedCtokenSource, func));
            }

            return await Task.WhenAll(res);
        }
    }

    internal class CommunicationException : Exception { }
    
    public class MaxRetriesException<T>:Exception {
        public readonly T Argument;
        public MaxRetriesException(string message, T argument) : base(message) {
            Argument = argument;
        }
    }
}