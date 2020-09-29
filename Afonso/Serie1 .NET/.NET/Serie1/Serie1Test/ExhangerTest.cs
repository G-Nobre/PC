using System;
using System.Threading;
using Utils;
using Ex3;
using NUnit.Framework;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Serie1Test {
    public class ExhangerTest {
        [Test]
        public void ShouldExchangeWithSuccess() {
            int numberOfThreads = 2;
            int timeout = 1000;
            int[] values = {2, 3};
            Thread[] threads = new Thread[numberOfThreads];
            Exchanger<int> exchanger = new Exchanger<int>();
            Optional<int>[] results = new Optional<int>[numberOfThreads];

            threads[0] = new Thread(() => {
                try {
                    results[0] = exchanger.Exchange(values[0], timeout);
                }
                catch (ThreadInterruptedException e) {
                    Console.WriteLine("ERROR : shouldn't throw exception");
                    Console.WriteLine(e.StackTrace);
                }
            });
            threads[1] = new Thread(() => {
                try {
                    results[1] = exchanger.Exchange(values[1], timeout);
                }
                catch (ThreadInterruptedException e) {
                    Console.WriteLine("ERROR : shouldn't throw exception");
                    Console.WriteLine(e.StackTrace);
                }
            });

            for (int i = 0; i < numberOfThreads; i++) {
                threads[i].Start();
            }

            for (int i = 0; i < numberOfThreads; i++) {
                threads[i].Join();
            }

            AreEqual(values[1], results[0].Value);
            AreEqual(values[0], results[1].Value);
        }

        [Test]
        public void ShouldGiveTimeout() {
            Exchanger<int> exchanger = new Exchanger<int>();
            int value = 2;
            int timeout = 300;
            Optional<int> result = default;
            Thread t = new Thread(() => {
                try {
                    result = exchanger.Exchange(value, timeout);
                }
                catch (ThreadInterruptedException e) {
                    Console.WriteLine("ERROR : shouldn't throw exception");
                    Console.WriteLine(e.StackTrace);
                }
            });

            t.Start();
            t.Join();
            IsTrue(result.IsEmpty);
        }
    }
}