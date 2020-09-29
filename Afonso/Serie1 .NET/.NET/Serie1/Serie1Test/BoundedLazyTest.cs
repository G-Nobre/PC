using System;
using System.Threading;
using Ex1;
using NUnit.Framework;
using Utils;
using static Serie1Test.TestUtils;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Serie1Test {
    class BoundedLazyTest {
        /**
         * DEBUG ONLY VARIABLES
         */
        private readonly string _testClass = "TEST CLASS - ";

        /**
         * End of DEBUG ONLY VARIABLES
         */
        private readonly int TIMEOUT = 2000;

        private const int NrOfThreads = 5;
        Thread[] _threads = new Thread[NrOfThreads];

        private readonly Func<long> _supplier = () => Thread.CurrentThread.ManagedThreadId;

        [Test]
        public void ShouldReturnValueAlreadyCreated() {
            int lives = 3;
            BoundedLazy<long> boundedLazy = new BoundedLazy<long>(_supplier, lives);
            Optional<long>[] value = {Optional<long>.Of(long.MaxValue)};

            CreateThreads(_threads, 2, () => {
                try {
                    value[0] = boundedLazy.Get(TIMEOUT);
                }
                catch (Exception) { }
            });

            _threads[0].Start();
            try {
                _threads[0].Join();
            }
            catch (ThreadInterruptedException e) {
                Console.WriteLine(e.StackTrace);
            }

            _threads[1].Start();
            try {
                _threads[1].Join();
            }
            catch (ThreadInterruptedException e) {
                Console.WriteLine(e.StackTrace);
            }

            Assert.AreEqual(_threads[0].ManagedThreadId, value[0].Value);
            _threads = ClearThreadArray(NrOfThreads);
        }

        [Test]
        public void ShouldExceedNumberOfLivesAndComputeValueAgain() {
            BoundedLazy<long> boundedLazy = new BoundedLazy<long>(_supplier, 2);
            Optional<long>[] values = {
                Optional<long>.Of(long.MaxValue),
                Optional<long>.Of(long.MaxValue),
                Optional<long>.Of(long.MaxValue)
            };
            int[] idx = {0};
            CreateThreads(_threads, 3, () => {
                try {
                    values[idx[0]++] = boundedLazy.Get(1000);
                }
                catch (Exception) { }
            });

            for (int i = 0; i < 3; i++) {
                _threads[i].Start();
                try {
                    _threads[i].Join();
                }
                catch (ThreadInterruptedException) { }
            }

            Assert.AreEqual(_threads[0].ManagedThreadId, (long) values[0].Value);
            Assert.AreEqual(_threads[0].ManagedThreadId, (long) values[1].Value);
            Assert.AreEqual(_threads[2].ManagedThreadId, (long) values[2].Value);
            _threads = ClearThreadArray(NrOfThreads);
        }

        [Test]
        public void ShouldReturnInterruptedException() {
            BoundedLazy<long> boundedLazy = new BoundedLazy<long>(() => {
                try {
                    Thread.Sleep(2000);
                }
                catch (Exception ignored) { }

                return 5L;
            }, 2);

            bool expected = false;
            _threads[0] = new Thread(() => {
                try {
                    boundedLazy.Get(TIMEOUT);
                }
                catch (Exception ignored) { }
            });
            _threads[1] = new Thread(() => {
                try {
                    Thread.Sleep(1000);
                    boundedLazy.Get(TIMEOUT);
                }
                catch (ThreadInterruptedException ignored) {
                    expected = true;
                }
                catch (Exception ignored) { }
            });

            _threads[0].Start();
            _threads[1].Start();

            _threads[1].Interrupt();

            for (int i = 0; i < 2; i++) {
                try {
                    _threads[i].Join();
                }
                catch (ThreadInterruptedException ignored) { }
            }

            Assert.IsTrue(expected);
        }
    }
}