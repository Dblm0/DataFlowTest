using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataFlowTest
{
    static class Helper
    {
        public static Task DelaySleep(int ms)
        {
            return Task.Run(() => { System.Threading.Thread.Sleep(ms); });
        }
    }
    static class DataFlowExtensions
    {
        public static async Task ProduceDataAsync(this ITargetBlock<int> block, int item, int count, bool completionRequired = false)
        {
            foreach (var i in Enumerable.Repeat(item, count))
            {
                await block.SendAsync(i).ConfigureAwait(false);
            }
            if (completionRequired) block.Complete();
        }
        public static async Task<MeasurmentsData> MeasureDataAsync(this ISourceBlock<int> block)
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

        public static async Task<MeasurmentsData> MeasureDataAsync(this ISourceBlock<int> block, int[] keys)
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
    }
    static class DelayedItemProducerExtensions
    {
        public static void ProduceData(this DelayedItemProducer<int> producer, int item, int count, bool completionRequired = false)
        {
            foreach (var i in Enumerable.Repeat(item, count))
            {
                producer.PostItems(i);
            }
            if (completionRequired) producer.Complete();
        }

        public static Task<MeasurmentsData> MeasureDataTask(this DelayedItemProducer<int>[] producers, int[] keys)
        {
            return Task.Run(() =>
            {
                var stopwatches = keys.ToDictionary(x => x, x => new Stopwatch());
                var seq = new List<double>();
                var stats = keys.ToDictionary(x => x, x => new List<double>());

                Queue<DelayedItemProducer<int>> _prodQueue = new(producers);

                while (_prodQueue.Count > 0)
                {
                    var prod = _prodQueue.Dequeue();
                    if (prod.TryReceve(out var x))
                    {
                        seq.Add(x);
                        stopwatches[x].Stop();
                        stats[x].Add(stopwatches[x].ElapsedMilliseconds);
                        stopwatches[x].Restart();
                    }
                    if (!prod.IsCompleted) _prodQueue.Enqueue(prod);
                }
                return new MeasurmentsData(stats, seq);
            });
        }
    }

}
