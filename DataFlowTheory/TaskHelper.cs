using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataFlowTheory
{
    public static class TaskHelper
    {
        public static Task Sleep(int delay)
        {
            Thread.Sleep(delay);
            return Task.CompletedTask;
        }
    }
}
