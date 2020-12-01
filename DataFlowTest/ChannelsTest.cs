using ChannelsTheory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Process p = Process.GetCurrentProcess();
            p.PriorityClass = ProcessPriorityClass.RealTime;
            _helper = new TestHelper(helper);
        }
        [Theory]
        [InlineData(20)]
        public async Task SingleDelayChannelTest(int delay)
        {
            var channel = new DelayChannel<int>(delay);
            var t = channel.Writer.ProduceDataAsync(0, 1500, completeRequired: true);
            var res = await channel.Reader.MeasureDataAsync();
            var delays = res.Delays[-1];
            OxyPlotExporter.ToPNG($"SingleDelayChannelTest_{delay}.png", $"target delay: {delay} ms", delays);
            _helper.AssertStats(delays, delay, delay + 10, skip: 3);
        }
        [Theory]
        [InlineData(20, 25, 30, 35, 40)]
        public async Task ParallelOneToOneDelayChannel(params int[] delays)
        {
            int N = delays.Length;
            Task<MeasurmentsData>[] measurments = new Task<MeasurmentsData>[N];
            for (int i = 0; i < N; i++)
            {
                var delay = delays[i];
                var channel = new DelayChannel<int>(delay);
                measurments[i] = channel.Reader.MeasureDataAsync();
                var t = channel.Writer.ProduceDataAsync(i, 400, completeRequired: true);
            }
            var measCollection = await Task.WhenAll(measurments);
            var statCollection = measCollection.Select(x => x.Delays[-1]).ToArray();
            OxyPlotExporter.ToPNG("ParallelOneToOneDelayChannel.png", $"target delays ({string.Join(", ", delays)}) ms", statCollection);
            foreach (var (delay, st) in delays.Zip(statCollection, (d, s) => (d, s)))
            {
                _helper.AssertStats(st, delay, delay * 0.1, skip: 10);
            }
        }
    }
}
