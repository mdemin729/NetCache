using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ZiggyCreatures.Caching.Fusion;

namespace NetCache.UnitTests
{
    [TestFixture]
    public class FusionTests
    {
        private static long _counter = 0;
        private FusionCache _cache;

        [SetUp]
        public void SetUp()
        {
            _cache = new FusionCache(new FusionCacheOptions());
        }
        
        [Test]
        public void StampedeProtectionTest()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                CancellationToken ct = cts.Token;
                var invocationsCount = 100;
                using (CountdownEvent countdown = new CountdownEvent(invocationsCount))
                {
                    Parallel.For(0, invocationsCount,
                        new ParallelOptions { CancellationToken = ct },
                        async (x, parallelLoopState) =>
                        {
                            await _cache.GetOrSetAsync($"TheSameKey",
                                async _ => await GetResultAsync(ct),
                                TimeSpan.FromSeconds(10));

                            countdown.Signal();
                        });

                    countdown.Wait(ct);
                }
            }
            
            Assert.AreEqual(1, Interlocked.Read(ref _counter),
                "The number of calls to GetResult should be 1");
        }

        private static async Task<long> GetResultAsync(CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
            return Interlocked.Increment(ref _counter);
        }
    }
}