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
        public void AssertStats(List<double> values, double baseLine, double drift, int skip = 0)
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
        public async Task ProduceDataAsync(ITargetBlock<int> block, int item, int count, int delay = 0)
        {
            foreach (var i in Enumerable.Repeat(item, count))
            {
                var delayTask = Task.Delay(delay).ConfigureAwait(false);
                await block.SendAsync(i).ConfigureAwait(false);
                await delayTask;
            }
        }
        public async Task ProduceDataAsync(ChannelWriter<int> channel, int item, int count, int delay = 0)
        {
            foreach (var i in Enumerable.Repeat(item, count))
            {
                var delayTask = Task.Delay(delay).ConfigureAwait(false);
                await channel.WriteAsync(i).ConfigureAwait(false);
                await delayTask;
            }
        }

        public async Task<MeasurmentsData> MeasureDataAsync(ISourceBlock<int> block)
        {
            var stats = new List<double>();
            var seq = new List<double>();
            Stopwatch sw = Stopwatch.StartNew();
            while (await block.OutputAvailableAsync().ConfigureAwait(false))
            {
                var item = block.Receive();
                seq.Add(item);
                sw.Stop();
                stats.Add(sw.ElapsedMilliseconds);
                sw.Restart();
            }
            return new MeasurmentsData(new Dictionary<int, List<double>> { [-1] = stats }, seq);
        }

        public async Task<MeasurmentsData> MeasureDataAsync(ISourceBlock<int> block, int[] keys)
        {
            var stopwatches = keys.ToDictionary(x => x, x => new Stopwatch());
            var seq = new List<double>();
            var stats = keys.ToDictionary(x => x, x => new List<double>());

            while (await block.OutputAvailableAsync().ConfigureAwait(false))
            {
                var x = block.Receive();
                seq.Add(x);
                stopwatches[x].Stop();
                stats[x].Add(stopwatches[x].ElapsedMilliseconds);
                stopwatches[x].Restart();
            }
            return new MeasurmentsData(stats, seq);
        }

        public async Task<MeasurmentsData> MeasureDataAsync(ChannelReader<int> channel)
        {
            var stats = new List<double>();
            var seq = new List<double>();
            Stopwatch sw = Stopwatch.StartNew();
            await foreach (int x in channel.ReadAllAsync().ConfigureAwait(false))
            {
                seq.Add(x);
                sw.Stop();
                stats.Add(sw.ElapsedMilliseconds);
                sw.Restart();
            }
            return new MeasurmentsData(new Dictionary<int, List<double>> { [-1] = stats }, seq);
        }
        public async Task<MeasurmentsData> MeasureDataAsync(ChannelReader<int> channel, int[] keys)
        {
            var stopwatches = keys.ToDictionary(x => x, x => new Stopwatch());
            var seq = new List<double>();
            var stats = keys.ToDictionary(x => x, x => new List<double>());

            await foreach (int x in channel.ReadAllAsync().ConfigureAwait(false))
            {
                seq.Add(x);
                stopwatches[x].Stop();
                stats[x].Add(stopwatches[x].ElapsedMilliseconds);
                stopwatches[x].Restart();
            }
            return new MeasurmentsData(stats, seq); ;
        }
    }

    class MeasurmentsData
    {
        public MeasurmentsData(IReadOnlyDictionary<int, List<double>> delays, List<double> sequence)
        {
            Delays = delays;
            Sequence = sequence;
        }

        public IReadOnlyDictionary<int, List<double>> Delays { get; }
        public List<double> Sequence { get; }
    }
}
