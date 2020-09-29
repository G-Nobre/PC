using System.Diagnostics;
using System.Threading;

namespace Serie1Test {
    public class TestUtils {
        public static void CreateThreads(Thread[] threads, int nr, ThreadStart runnable) {
            for (int i = 0; i < nr; i++) {
                threads[i] = new Thread(runnable);
                threads[i].Priority = ThreadPriority.Highest;
            }
        }

        public static Thread[] ClearThreadArray(int nrOfThreads) => new Thread[nrOfThreads];

        public static void CreateAndStartThread(Thread[] threads, int nrOfThreads, ThreadStart function) {
            for (var i = 0; i < nrOfThreads; i++) {
                threads[i] = new Thread(function);
                threads[i].Start();
            }
        }
    }
}