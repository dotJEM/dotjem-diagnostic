using System;
using System.Threading.Tasks;
using DotJEM.Diagnostic.Common;
using DotJEM.Diagnostic.Model;

namespace DotJEM.Diagnostic.Writers
{
    public interface ITraceWriter : IDisposable
    {
        Task WriteAsync(TraceEvent trace);
        Task FlushAsync();
    }

    public class ConsoleWriter : Disposable, ITraceWriter
    {
        private readonly ITraceEventFormatter formatter;

        public ConsoleWriter(ITraceEventFormatter formatter = null)
        {
            this.formatter = formatter ?? new DefaultTraceEventFormatter();
        }

        //TODO: Formatter.
        public Task WriteAsync(TraceEvent trace) 
            => Task.Run(() => Console.WriteLine(formatter.Format(trace)));

        public Task FlushAsync() => Task.CompletedTask;
    }
}