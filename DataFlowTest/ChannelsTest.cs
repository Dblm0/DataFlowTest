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
    }
}
