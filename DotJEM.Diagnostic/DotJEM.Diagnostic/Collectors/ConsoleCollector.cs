using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Diagnostic.Writers;

namespace DotJEM.Diagnostic.Collectors
{
    public interface ITraceEventCollector
    {
        Task Collect(TraceEvent trace);
    }

    //TODO: This is a temp collector, collectors are just meant to branch events to different writers, this should be a console writer 
    public class ConsoleTraceEventCollector : ITraceEventCollector
    {
        public async Task Collect(TraceEvent trace) => await Task.Run(()=>Console.WriteLine(trace)).ConfigureAwait(false);
    }

    public class CompositeTraceEventCollector : ITraceEventCollector
    {
        private readonly List<ITraceEventCollector> collectors = new List<ITraceEventCollector>();

        public CompositeTraceEventCollector(params ITraceEventCollector[] collectors) 
            : this(collectors.AsEnumerable())
        {
        }

        public CompositeTraceEventCollector(IEnumerable<ITraceEventCollector> collectors)
        {
            this.collectors = collectors.ToList();
        }
        public async Task Collect(TraceEvent trace) => await Task.WhenAll(collectors.Select(collector => collector.Collect(trace)));
    }

    public class TraceEventCollector : ITraceEventCollector
    {
        private ITraceWriter writer;

        public TraceEventCollector(ITraceWriter writer)
        {
            this.writer = writer;
        }

        public async Task Collect(TraceEvent trace) => await writer.Write(trace);
    }

}
