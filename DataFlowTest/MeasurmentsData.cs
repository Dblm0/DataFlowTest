using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlowTest
{
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
