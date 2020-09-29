using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compute;
using NUnit.Framework;

namespace TestSerie3 {
    [TestFixture]
    public class ComputeAndOperTests {
        [Test]
        public async Task ShouldComputeValuesCorrectly() {
            string[] elems = new string[100];
            int[] results = new int[100];
            Random random = new Random();
            for (int i = 0; i < elems.Length; i++) {
                elems[i] = RandomString(random.Next(1, 10));
                results[i] = elems[i].Length;
            }

            var withAwaitResults = default(int[]);
            try {
                withAwaitResults = await
                    ComputeAndOper<string, int>.ComputeAsync(
                        elems,
                        2,
                        new CancellationToken(),
                        str => str.Length);
            } catch (MaxRetriesException<string>) {
                Console.WriteLine(withAwaitResults);
                results = null;
            }
            
            Assert.AreEqual(results,withAwaitResults);
        }

        public static string RandomString(int length) {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}