using System;
using System.Threading;
using Ex4;
using NUnit.Framework;

namespace Serie1Test {
    public class TransferQueueTest {
        [Test]
        public void SuccessfulPutAndTake() {
            TransferQueue<int> transferQueue = new TransferQueue<int>();
            int numberOfThreads = 4;
            int timeout = 50;
            int message = 35;
            Thread[] threads = new Thread[numberOfThreads];

            for (int i = 0; i < numberOfThreads / 2; i++) {
                threads[i] = new Thread(() => transferQueue.Put(message));
            }

            int[] msgs = new int[2];
            int idx = default;
            for (int i = 2; i < numberOfThreads; i++) {
                threads[i] = new Thread(() => {
                    try {
                        Thread.Sleep(10);
                        msgs[idx++] = transferQueue.Take(timeout);
                    }
                    catch (ThreadInterruptedException e) {
                        Console.WriteLine("Error : take exception");
                        Console.WriteLine(e.StackTrace);
                    }
                });
            }

            threads[0].Start(); //put message
            Thread.Sleep(1);
            threads[2].Start(); // take message
            Thread.Sleep(1);
            threads[3].Start(); // take message - blocking
            Thread.Sleep(1);
            threads[1].Start(); // put message
            Thread.Sleep(1);

            for (int i = 0; i < numberOfThreads; i++) {
                threads[i].Join();
            }

            for (int i = 2; i < numberOfThreads; i++) {
                Assert.AreEqual(message, msgs[i - 2]);
            }
        }

        [Test]
        public void SuccessfulTransferAndTake() {
            TransferQueue<int> transferQueue = new TransferQueue<int>();
            int numberOfThreads = 4;
            int timeout = 50;
            int message = 35;
            Thread[] threads = new Thread[numberOfThreads];

            for (int i = 0; i < numberOfThreads / 2; i++) {
                threads[i] = new Thread(() => {
                    try {
                        transferQueue.Transfer(message, timeout);
                    }
                    catch (ThreadInterruptedException e) {
                        Console.WriteLine("ERROR : transfer exception");
                        Console.WriteLine(e.StackTrace);
                    }
                });
            }

            int msg = default;
            for (int i = 2; i < numberOfThreads; i++) {
                threads[i] = new Thread(() => {
                    try {
                        Thread.Sleep(10);
                        msg = transferQueue.Take(timeout);
                    }
                    catch (ThreadInterruptedException e) {
                        Console.WriteLine("ERROR : take exception");
                        Console.WriteLine(e.StackTrace);
                    }
                });
            }

            for (int i = 0; i < numberOfThreads; i++) {
                threads[i].Start();
            }

            for (int i = 0; i < numberOfThreads; i++) {
                threads[i].Join();
            }

            Assert.AreEqual(message, msg);
        }

        [Test]
        public void ShouldTimeoutAndReturnNullForTakeAndFalseForTransfer() {
            TransferQueue<int?> transferQueue = new TransferQueue<int?>();
            int numberOfThreads = 3;
            int timeout = 10;
            int message = 35;
            Thread[] threads = new Thread[numberOfThreads];

            threads[0] = new Thread(() => {
                try {
                    Thread.Sleep(100);
                    transferQueue.Put(message);
                }
                catch (ThreadInterruptedException e) {
                    Console.WriteLine(e.StackTrace);
                }
            });

            int? takeIntegerResult = default;
            threads[1] = new Thread(() => {
                try {
                    takeIntegerResult = transferQueue.Take(timeout);
                }
                catch (ThreadInterruptedException e) {
                    Console.WriteLine("ERROR : take exception");
                    Console.WriteLine(e.StackTrace);
                }
            });

            for (int i = 0; i < numberOfThreads - 1; i++) {
                threads[i].Start();
            }

            for (int i = 0; i < numberOfThreads - 1; i++) {
                threads[i].Join();
            }

            Assert.IsNull(takeIntegerResult);
        }
    }
}