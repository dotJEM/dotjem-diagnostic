using System;
using System.Threading.Tasks;

namespace DotJEM.Diagnostic.Writers
{
    public interface ITraceWriter : IDisposable
    {
        Task Write(TraceEvent trace);
        Task Flush();
    }
}