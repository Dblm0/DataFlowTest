using ChannelsTheory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DataFlowTest
{
    public class ChannelsTest
    {
        TestHelper _helper;
        public ChannelsTest(ITestOutputHelper helper)
        {
            _helper = new TestHelper(helper);
        }
        [Fact]
        public async Task MultipleProducersChannelTest()
        {
            int N = 4;
            int delayShift = 10;
            int baseDelay = 20;
            var channel = Channel.CreateBounded<int>(1);
            Task[] producers = new Task[N];
            for (int i = 0; i < N; i++)
            {
                producers[i] = _helper.ProduceDataAsync(channel, i, 300, baseDelay + i * delayShift);
            }
            var prods = Task.WhenAll(producers).ContinueWith(x => channel.Writer.Complete());
            var keys = Enumerable.Range(0, N).ToArray();
            var stats = await _helper.MeasureDelaysAsync(channel, keys);
            var statCollection = stats.Values.ToArray();
            OxyPlotExporter.ToPNG("MultipleProducersChannelTest.png", $"target delays from {baseDelay} to {baseDelay + (N - 1) * delayShift} ms", statCollection);
            foreach (var kvp in stats)
            {
                var calcDelay = baseDelay + kvp.Key * delayShift;
                _helper.AssertStats(kvp.Value, calcDelay, calcDelay * 0.1, skip: 10);
            }
        }
        [Theory]
        [InlineData(20, 40)]
        public async Task SingleManyToOneChannel(params int[] delays)
        {
            int N = delays.Length;
            Task[] producers = new Task[N];
            var channel = Channel.CreateBounded<int>(1);
            var keys = Enumerable.Range(0, N).ToArray();
            for (int i = 0; i < N; i++)
            {
                producers[i] = _helper.ProduceDataAsync(channel, i, 500, delays[i]);
            }
            var stats = _helper.MeasureDelaysAsync(channel, keys);
            var prods = Task.WhenAll(producers).ContinueWith(x => channel.Writer.Complete());
            var statResult = await stats;
            var statCollection = statResult.Values.ToArray();
            OxyPlotExporter.ToPNG("SingleManyToOneChannel.png", $"target delays ({string.Join(", ", delays)}) ms", statCollection);
            foreach (var (delay, st) in delays.Zip(statCollection, (d, s) => (d, s)))
            {
                _helper.AssertStats(st, delay, delay * 0.1, skip: 10);
            }
        }
        [Theory]
        [InlineData(20, 40)]
        public async Task ParallelOneToOneChannel(params int[] delays)
        {
            int N = delays.Length;
            Task<IList<double>>[] stats = new Task<IList<double>>[N];
            for (int i = 0; i < N; i++)
            {
                var delay = delays[i];
                var channel = Channel.CreateBounded<int>(1);
                stats[i] = _helper.MeasureDelaysAsync(channel);
                var t = _helper.ProduceDataAsync(channel, i, 100, delay).ContinueWith(x => channel.Writer.Complete());
            }
            var statCollection = await Task.WhenAll(stats);
            OxyPlotExporter.ToPNG("ParallelOneToOneChannel.png", $"target delays ({string.Join(", ", delays)}) ms", statCollection);
            foreach (var (delay, st) in delays.Zip(statCollection, (d, s) => (d, s)))
            {
                _helper.AssertStats(st, delay, delay * 0.1, skip: 10);
            }
        }
        [Theory]
        [InlineData(20, 25, 30)]
        public async Task ParallelOneToOneDelayChannel(params int[] delays)
        {
            int N = delays.Length;
            Task<IList<double>>[] stats = new Task<IList<double>>[N];
            for (int i = 0; i < N; i++)
            {
                var delay = delays[i];
                var channel = new DelayChannel<int>(delay);
                stats[i] = _helper.MeasureDelaysAsync(channel);
                var t = _helper.ProduceDataAsync(channel, i, 750).ContinueWith(x => channel.Writer.Complete());
            }
            var statCollection = await Task.WhenAll(stats);
            OxyPlotExporter.ToPNG("ParallelOneToOneDelayChannel.png", $"target delays ({string.Join(", ", delays)}) ms", statCollection);
            foreach (var (delay, st) in delays.Zip(statCollection, (d, s) => (d, s)))
            {
                _helper.AssertStats(st, delay, delay * 0.1, skip: 10);
            }
        }
    }
}
