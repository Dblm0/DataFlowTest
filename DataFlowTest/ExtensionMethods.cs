﻿using System;
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
}
