using DotJEM.Diagnostic.Model;

namespace DotJEM.Diagnostic.Writers
{
    public interface ITraceEventFormatter
    {
        string Format(TraceEvent evt);
    }

    public class DefaultTraceEventFormatter : ITraceEventFormatter
    {
        public string Format(TraceEvent evt) => evt.ToString();
    }
}