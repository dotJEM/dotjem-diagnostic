using System;
using System.Threading.Tasks;
using DotJEM.Diagnostic.Common;
using DotJEM.Diagnostic.Model;
using DotJEM.Diagnostic.Writers.Formatter;

namespace DotJEM.Diagnostic.Writers
{
    public interface ITraceWriter<TEvent> : IDisposable
    {
        Task Write(TEvent trace);
        Task AsyncFlush();
    }

    public class ConsoleWriter<TEvent> : Disposable, ITraceWriter<TEvent>
    {
        private readonly ITraceFormatter<TEvent> formatter;

        public ConsoleWriter(ITraceFormatter<TEvent> formatter = null)
        {
            this.formatter = formatter ?? new DefaultTraceFormatter<TEvent>();
        }

        public Task Write(TEvent trace) => Task.Run(() => Console.WriteLine(formatter.Format(trace)));
        public Task AsyncFlush() => Task.CompletedTask;
    }
}