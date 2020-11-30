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
        public void AssertStats(List<double> values, double baseLine, double upperLim, int skip = 0)
        {
            var skipped = values.Skip(skip);
            var max = skipped.Max();
            var min = skipped.Min();
            var maxRestriction = (Value: max, UpperLim: upperLim, LowerLim: baseLine);
            var minRestriction = (Value: min, UpperLim: baseLine, LowerLim: baseLine);  //strict minimum control
            _helper.WriteLine($"Target = {baseLine}, (min:{min}; max:{max})");

            foreach (var restrict in new[] { minRestriction, maxRestriction })
            {
                try
                {
                    Assert.InRange(restrict.Value, restrict.LowerLim, restrict.UpperLim);
                }
                catch
                {
                    int extremeIdx = values.IndexOf(restrict.Value);
                    _helper.WriteLine($"area around extreme value: {string.Join("; ", values.Skip(extremeIdx - 4).Take(8))}");
                    throw;
                }
            }
        }
    }
}
