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
