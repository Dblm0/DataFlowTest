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
    public class BuffersTest
    {
        TestHelper _helper;
        public BuffersTest(ITestOutputHelper helper)
        {
            _helper = new TestHelper(helper);
        }

        [Theory]
        [InlineData(75)]
        [InlineData(20)]
        public async Task SingleDelayBlockTest(int delay)
        {
            var block = new DelayBlock<int>(delay);
            var t = _helper.ProduceDataAsync(block, 0, 300).ContinueWith(x => block.Complete());
            var res = await _helper.MeasureDelaysAsync(block);
            OxyPlotExporter.ToPNG($"SingleDelayBlockTest_{delay}.png", $"target delay: {delay} ms", res);
            _helper.AssertStats(res, delay, delay * 0.1, skip: 3);
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
                var t = _helper.ProduceDataAsync(block, i, 300).ContinueWith(x => block.Complete());
                producers[i] = block.Completion;
            }
            var keys = Enumerable.Range(0, N).ToArray();
            var statTask = _helper.MeasureDelaysAsync(finalBlock, keys);
            var prods = Task.WhenAll(producers).ContinueWith(x => finalBlock.Complete());

            var stats = await statTask;
            var statCollection = stats.Values.ToArray();
            OxyPlotExporter.ToPNG("MultipleProducersBufferTest.png", $"target delays ({string.Join(", ", delays)}) ms", statCollection);
            foreach (var kvp in stats)
            {
                var calcDelay = delays[kvp.Key];
                _helper.AssertStats(kvp.Value, calcDelay, calcDelay * 0.1, skip: 10);
            }
        }

        [Theory]
        [InlineData(20, 25, 30, 35)]
        public async Task ParallelOneToOneBufferBlock(params int[] delays)
        {
            int N = delays.Length;
            Task<IList<double>>[] stats = new Task<IList<double>>[N];
            for (int i = 0; i < N; i++)
            {
                var delay = delays[i];
                var block = new BufferBlock<int>(new() { BoundedCapacity = 1 });
                stats[i] = _helper.MeasureDelaysAsync(block);
                var t = _helper.ProduceDataAsync(block, i, 1000, delay).ContinueWith(x => block.Complete());
            }
            var statCollection = await Task.WhenAll(stats);
            OxyPlotExporter.ToPNG("ParallelOneToOneBufferBlock.png", $"target delays ({string.Join(", ", delays)}) ms", statCollection);
            foreach (var (delay, st) in delays.Zip(statCollection, (d, s) => (d, s)))
            {
                _helper.AssertStats(st, delay, delay * 0.1, skip: 10);
            }
        }

        [Theory]
        [InlineData(20, 25, 30, 35, 40, 45)]
        public async Task SingleManyToOneBufferBlock(params int[] delays)
        {
            int N = delays.Length;
            Task[] producers = new Task[N];
            var block = new BufferBlock<int>(new() { BoundedCapacity = 1 });
            var keys = Enumerable.Range(0, N).ToArray();
            var statsTask = _helper.MeasureDelaysAsync(block, keys);
            for (int i = 0; i < N; i++)
            {
                var delay = delays[i];
                producers[i] = _helper.ProduceDataAsync(block, i, 1000, delay);
            }
            var prods = Task.WhenAll(producers).ContinueWith(x => block.Complete());
            var statResult = await statsTask;
            var statCollection = statResult.Values.ToArray();
            OxyPlotExporter.ToPNG("SingleManyToOneBufferBlock.png", $"target delays ({string.Join(", ", delays)}) ms", statCollection);
            foreach (var (delay, st) in delays.Zip(statCollection, (d, s) => (d, s)))
            {
                _helper.AssertStats(st, delay, delay * 0.1, skip: 10);
            }
        }
        [Theory]
        [InlineData(20, 25, 30, 35)]
        public async Task ParallelOneToOneDelayBlock(params int[] delays)
        {
            int N = delays.Length;
            Task<IList<double>>[] stats = new Task<IList<double>>[N];
            for (int i = 0; i < N; i++)
            {
                var delay = delays[i];
                var block = new DelayBlock<int>(delay);
                stats[i] = _helper.MeasureDelaysAsync(block);
                var t = _helper.ProduceDataAsync(block, i, 100).ContinueWith(x => block.Complete());
            }
            var statCollection = await Task.WhenAll(stats);
            OxyPlotExporter.ToPNG("ParallelOneToOneDelayBlock.png", $"target delays ({string.Join(", ", delays)}) ms", statCollection);
            foreach (var (delay, st) in delays.Zip(statCollection, (d, s) => (d, s)))
            {
                _helper.AssertStats(st, delay, delay * 0.1, skip: 10);
            }
        }
    }
}
