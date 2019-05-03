using System;
using System.Threading.Tasks;
using DotJEM.Diagnostic.Model;

namespace DotJEM.Diagnostic.Writers
{
    public interface ITraceWriter : IDisposable
    {
        Task Write(TraceEvent trace);
        Task AsyncFlush();
    }

    public class ConsoleWriter : Disposable, ITraceWriter
    {
        private readonly ITraceEventFormatter formatter;

        public ConsoleWriter(ITraceEventFormatter formatter = null)
        {
            this.formatter = formatter ?? new DefaultTraceEventFormatter();
        }

        //TODO: Formatter.
        public async Task Write(TraceEvent trace) 
            => await Task.Run(() => Console.WriteLine(formatter.Format(trace)))
                .ConfigureAwait(false);

        public async Task AsyncFlush() => await Task.CompletedTask.ConfigureAwait(false);
    }
}