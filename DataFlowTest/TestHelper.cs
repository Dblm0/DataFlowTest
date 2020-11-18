using DataFlowTheory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Xunit;
using Xunit.Abstractions;

namespace DataFlowTest
{
    class TestHelper
    {
        ITestOutputHelper _helper;
        public TestHelper(ITestOutputHelper helper)
        {
            _helper = helper;
        }
        public void AssertStats(IList<double> values, double baseLine, double drift, int skip = 0)
        {
            var skipped = values.Skip(skip);
            var max = skipped.Max();
            var min = skipped.Min();
            _helper.WriteLine($"Target = {baseLine}, (min:{min}; max:{max})");

            foreach (var extreme in new[] { min, max })
            {
                try
                {
                    Assert.InRange(extreme, baseLine - drift, baseLine + drift);
                }
                catch
                {
                    int extremeIdx = values.IndexOf(extreme);
                    _helper.WriteLine($"area around extreme value: {string.Join("; ", values.Skip(extremeIdx - 4).Take(8))}");
                    throw;
                }
            }
        }
        public async Task<IList<double>> MeasureDelaysAsync(ISourceBlock<int> block)
        {
            Stopwatch sw = new Stopwatch();
            var stats = new List<double>();

            while (await block.OutputAvailableAsync().ConfigureAwait(false))
            {
                block.Receive();
                sw.Stop();
                stats.Add(sw.ElapsedMilliseconds);
                sw.Restart();
            }
            return stats;
        }
        public async Task ProduceDataAsync(ITargetBlock<int> block, int item, int count, int delay = 0)
        {
            foreach (var i in Enumerable.Repeat(item, count))
            {
                await block.SendAsync(i);
                await TaskHelper.Sleep(delay);
            }
        }

        public async Task ProduceDataAsync(ChannelWriter<int> channel, int item, int count, int delay)
        {
            foreach (var i in Enumerable.Repeat(item, count))
            {
                await channel.WriteAsync(i);
                await TaskHelper.Sleep(delay);
            }
        }

        public async Task<Dictionary<int, List<double>>> MeasureDelaysAsync(ISourceBlock<int> block, int[] keys)
        {
            var stopwatches = keys.ToDictionary(x => x, x => new Stopwatch());
            var stats = keys.ToDictionary(x => x, x => new List<double>());

            while (await block.OutputAvailableAsync().ConfigureAwait(false))
            {
                var x = block.Receive();
                stopwatches[x].Stop();
                stats[x].Add(stopwatches[x].ElapsedMilliseconds);
                stopwatches[x].Restart();
            }
            return stats;
        }
        public async Task<Dictionary<int, List<double>>> MeasureDelaysAsync(ChannelReader<int> channel, int[] keys)
        {
            var stopwatches = keys.ToDictionary(x => x, x => new Stopwatch());
            var stats = keys.ToDictionary(x => x, x => new List<double>());

            await foreach (int x in channel.ReadAllAsync().ConfigureAwait(false))
            {
                stopwatches[x].Stop();
                stats[x].Add(stopwatches[x].ElapsedMilliseconds);
                stopwatches[x].Restart();
            }
            return stats;
        }
    }
}
