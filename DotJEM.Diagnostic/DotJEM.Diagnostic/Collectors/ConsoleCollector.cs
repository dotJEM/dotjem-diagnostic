using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotJEM.Diagnostic.Collectors
{
    public interface ITraceEventCollector
    {
        Task Collect(TraceEvent trace);
    }

    public class ConsoleTraceEventCollector : ITraceEventCollector
    {
        public async Task Collect(TraceEvent trace) => await Task.Run(()=>Console.WriteLine(trace)).ConfigureAwait(false);
    }
}
