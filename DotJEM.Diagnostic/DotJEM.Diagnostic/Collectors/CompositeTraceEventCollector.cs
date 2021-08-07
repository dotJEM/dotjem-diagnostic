using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Diagnostic.Model;

namespace DotJEM.Diagnostic.Collectors
{
    public class CompositeTraceEventCollector<TEvent> : ITraceCollector<TEvent>
    {
        private readonly List<ITraceCollector<TEvent>> collectors;

        public CompositeTraceEventCollector(params ITraceCollector<TEvent>[] collectors) 
            : this(collectors.AsEnumerable())
        {
        }

        public CompositeTraceEventCollector(IEnumerable<ITraceCollector<TEvent>> collectors)
        {
            this.collectors = collectors.ToList();
        }
        public Task Collect(TEvent trace) => Task.WhenAll(collectors.Select(collector => collector.Collect(trace)));
    }
}
