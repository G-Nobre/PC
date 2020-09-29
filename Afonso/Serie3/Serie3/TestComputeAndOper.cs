using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Compute {
    [TestClass]
    public class TestComputeAndOper {
        [TestMethod]
        public async void ShouldComputeValuesCorrectly() {
            string[] elems = {"isel", "guida", "eu"};
            int[] results = {4, 5, 2};

            var firstResult = ComputeAndOper<string, int>
                .ComputeAsync(
                    elems,
                    5,
                    new CancellationToken(),
                    (str) => str.Length);
            
            var secondResult = await 
                ComputeAndOper<string, int>.ComputeAsync(
                    elems,
                    5,
                    new CancellationToken(),
                    (str) => str.Length);
            
            Console.WriteLine(firstResult.Result);
            Assert.Equals(results, secondResult);
        }
    }
}