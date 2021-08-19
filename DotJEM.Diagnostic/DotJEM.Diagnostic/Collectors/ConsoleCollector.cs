using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Diagnostic.Model;
using DotJEM.Diagnostic.Writers;

namespace DotJEM.Diagnostic.Collectors
{
    public interface ITraceEventCollector 
    {
        Task Collect(TraceEvent trace);
    }

    public class CompositeTraceEventCollector : ITraceEventCollector
    {
        private readonly List<ITraceEventCollector> collectors;

        public CompositeTraceEventCollector(params ITraceEventCollector[] collectors) 
            : this(collectors.AsEnumerable()) { }

        public CompositeTraceEventCollector(IEnumerable<ITraceEventCollector> collectors)
        {
            this.collectors = collectors.ToList();
        }

        public Task Collect(TraceEvent trace) => Task.WhenAll(collectors.Select(collector => collector.Collect(trace)));
    }

    public class TraceEventCollector : ITraceEventCollector
    {
        private readonly ITraceWriter writer;

        public TraceEventCollector(ITraceWriter writer)
        {
            this.writer = writer;
        }

        public Task Collect(TraceEvent trace) => writer.WriteAsync(trace);
    }

    public class NullTraceEventCollector : ITraceEventCollector
    {
        public Task Collect(TraceEvent trace)
        {
            return Task.CompletedTask;
        }
    }
}
