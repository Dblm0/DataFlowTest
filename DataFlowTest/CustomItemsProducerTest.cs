using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DataFlowTest
{
    public class CustomItemsProducerTest
    {
        TestHelper _helper;
        public CustomItemsProducerTest(ITestOutputHelper helper)
        {
            Process p = Process.GetCurrentProcess();
            p.PriorityClass = ProcessPriorityClass.Normal;
            _helper = new TestHelper(helper);
        }

        [Theory]
        [InlineData(20, 20, 20, 20, 20, 20)]
        public async Task MultipleProducersBufferTest(params int[] delays)
        {
            int N = delays.Length;
            DelayedItemProducer<int>[] prods = new DelayedItemProducer<int>[N];
            for (int i = 0; i < N; i++)
            {
                var item = i;
                var producer = new DelayedItemProducer<int>(delays[item]);
                await Task.Run(() => producer.ProduceData(item, 500, completionRequired: true));
                prods[item] = producer;
            }
            var keys = Enumerable.Range(0, N).ToArray();
            var meas = await prods.MeasureDataTask(keys);
            OxyPlotExporter.ToPNG("MultipleCustomItemsProducerTest.png", $"target delays ({string.Join(", ", delays)}) ms", meas);
            foreach (var kvp in meas.Delays)
            {
                var calcDelay = delays[kvp.Key];
                _helper.AssertStats(kvp.Value, calcDelay, calcDelay * 0.1, skip: 10);
            }
        }
    }
}
