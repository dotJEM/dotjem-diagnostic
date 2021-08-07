using System.Threading.Tasks;
using DotJEM.Diagnostic.Writers;

namespace DotJEM.Diagnostic.Collectors
{
    public class TraceEventCollector<TEvent> : ITraceCollector<TEvent>
    {
        private readonly ITraceWriter<TEvent> writer;

        public TraceEventCollector(ITraceWriter<TEvent> writer)
        {
            this.writer = writer;
        }

        public Task Collect(TEvent trace) => writer.Write(trace);
    }
}