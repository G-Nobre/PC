using System;
using System.Threading;
using Ex2;
using Utils;
using NUnit.Framework;
using static Serie1Test.TestUtils;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Serie1Test {
    public class BroadcastBoxTest {
        private static readonly int NR_OF_THREADS = 5;
        private Thread[] _threads = new Thread[NR_OF_THREADS];

        private readonly int TIMEOUT = 500;

        [Test]
        public void ShouldReturnTheMessagesWithSuccess() {
            var message = "Thread with id %s presenting a message";
            var finalMessage = "";
            BroadcastBox<string> broadcastBox = new BroadcastBox<string>();
            var messagesReceived = new Optional<string>[3];
            var index = 0;
            var nrOfThreadsWhoGotTheMessage = 0;

            CreateAndStartThread(_threads, 3, () => {
                try {
                    messagesReceived[index++] = broadcastBox.receive(TIMEOUT);
                }
                catch (ThreadInterruptedException e) {
                    Console.WriteLine(e.StackTrace);
                }
            });

            _threads[3] = new Thread(() => {
                finalMessage = message
                    .Replace(
                        "%s",
                        Thread.CurrentThread.ManagedThreadId.ToString()
                    );
                nrOfThreadsWhoGotTheMessage = broadcastBox.DeliverToAll(finalMessage);
            });
            _threads[3].Start();
            for (var i = 0; i < 3; i++)
                try {
                    _threads[i].Join();
                }
                catch (ThreadInterruptedException e) {
                    Console.WriteLine(e.StackTrace);
                }

            AreEqual(3, nrOfThreadsWhoGotTheMessage);
            foreach (Optional<string> optional in messagesReceived) {
                IsTrue(optional.IsPresent);
                AreEqual(finalMessage, optional.Value);
            }

            _threads = ClearThreadArray(NR_OF_THREADS);
        }

        [Test]
        public void ShouldExceedTimeoutAndReturnEmptyWithZeroDeliveredMessages() {
            BroadcastBox<string> broadcastBox = new BroadcastBox<string>();
            Optional<string> result = Optional<string>.Empty();
            int nrOfmessagesDelivered = 0;
            _threads[0] = new Thread(() => {
                try {
                    Thread.Sleep(1000);
                }
                catch (ThreadInterruptedException e) {
                    Console.WriteLine(e.StackTrace);
                }

                nrOfmessagesDelivered = broadcastBox.DeliverToAll("To be ignored");
            });

            _threads[1] = new Thread(() => {
                try {
                    result = broadcastBox.receive(TIMEOUT);
                }
                catch (ThreadInterruptedException e) {
                    Console.WriteLine(e.StackTrace);
                }
            });

            _threads[1].Start();
            _threads[0].Start();

            try {
                _threads[1].Join();
                _threads[0].Join();
            }
            catch (ThreadInterruptedException e) {
                Console.WriteLine(e.StackTrace);
            }

            IsTrue(result.IsEmpty);
            AreEqual(0, nrOfmessagesDelivered);

            _threads = ClearThreadArray(NR_OF_THREADS);
        }
    }
}