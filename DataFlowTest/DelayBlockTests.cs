using DataFlowTheory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Xunit;
using Xunit.Abstractions;

namespace DataFlowTest
{
    public class DelayBlockTests
    {
        TestHelper _helper;
        public DelayBlockTests(ITestOutputHelper helper)
        {
            Process p = Process.GetCurrentProcess();
            p.PriorityClass = ProcessPriorityClass.RealTime;
            _helper = new TestHelper(helper);
        }

        [Theory]
        [InlineData(75)]
        [InlineData(20)]
        public async Task SingleDelayBlockTest(int delay)
        {
            var block = new DelayBlock<int>(delay);
            var t = block.ProduceDataAsync(0, 1500).ContinueWith(x => block.Complete());
            var res = await block.MeasureDataAsync();
            var delays = res.Delays[-1];
            OxyPlotExporter.ToPNG($"SingleDelayBlockTest_{delay}.png", $"target delay: {delay} ms", delays);
            _helper.AssertStats(delays, delay, delay + 10, skip: 3);
        }
        [Theory]
        [InlineData(20, 30, 40)]
        public async Task MultipleProducersBufferTest(params int[] delays)
        {
            int N = delays.Length;
            Task[] producers = new Task[N];
            var finalBlock = new BufferBlock<int>(new() { BoundedCapacity = 1 });
            for (int i = 0; i < N; i++)
            {
                var block = new DelayBlock<int>(delays[i]);
                block.LinkTo(finalBlock, new() { PropagateCompletion = false });
                var t = block.ProduceDataAsync(i, 300).ContinueWith(x => block.Complete());
                producers[i] = block.Completion;
            }
            var keys = Enumerable.Range(0, N).ToArray();
            var measTask = finalBlock.MeasureDataAsync(keys);
            var prods = Task.WhenAll(producers).ContinueWith(x => finalBlock.Complete());
            var meas = await measTask;

            var delaysCollection = meas.Delays.Values.ToArray();
            OxyPlotExporter.ToPNG("MultipleProducersBufferTest.png", $"target delays ({string.Join(", ", delays)}) ms", delaysCollection);
            foreach (var kvp in meas.Delays)
            {
                var calcDelay = delays[kvp.Key];
                _helper.AssertStats(kvp.Value, calcDelay, calcDelay * 0.1, skip: 10);
            }
        }

        [Theory]
        [InlineData(20, 25, 30, 35)]
        public async Task ParallelOneToOneDelayBlock(params int[] delays)
        {
            int N = delays.Length;
            Task<MeasurmentsData>[] measurments = new Task<MeasurmentsData>[N];
            for (int i = 0; i < N; i++)
            {
                var delay = delays[i];
                var block = new DelayBlock<int>(delay);
                measurments[i] = block.MeasureDataAsync();
                var t = block.ProduceDataAsync(i, 100).ContinueWith(x => block.Complete());
            }
            var measCollection = await Task.WhenAll(measurments);
            var statCollection = measCollection.Select(x => x.Delays[-1]).ToArray();
            OxyPlotExporter.ToPNG("ParallelOneToOneDelayBlock.png", $"target delays ({string.Join(", ", delays)}) ms", statCollection);
            foreach (var (delay, st) in delays.Zip(statCollection, (d, s) => (d, s)))
            {
                _helper.AssertStats(st, delay, delay * 0.1, skip: 10);
            }
        }
    }
}
