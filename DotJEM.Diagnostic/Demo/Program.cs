using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Diagnostic;
using DotJEM.Diagnostic.Collectors;
using DotJEM.Diagnostic.Correlation;
using DotJEM.Diagnostic.DataProviders;
using DotJEM.Diagnostic.Model;
using DotJEM.Diagnostic.Writers;
using DotJEM.Diagnostic.Writers.Archivers;
using DotJEM.Diagnostic.Writers.NonBlocking;
using Newtonsoft.Json.Linq;

namespace Demo
{
    class Program
    {
        private class RandomProvider : ICustomDataProvider, ICustomDataFormatter
        {
            public CustomData Generate(string key) => new CustomData(rnd.Next(), this);
            public string Format(CustomData customData) => $"{customData.Value:D9}";
        }
        private static Random rnd = new Random();
        protected internal const int ITTERATIONS = 1000000 /4;
        private static ILogger _logger;

        static void Main(string[] args)
        {
            ITraceWriter writer;

            Directory.CreateDirectory("logs");
            var collector = new CompositeTraceEventCollector(
                new TraceEventCollector(new ConsoleWriter()),
                new TraceEventCollector(writer = new QueuingTraceWriter("logs\\trace.log", 1024*1024*4, 10, true, new DefaultTraceEventFormatter())),
                new TraceEventCollector(writer = new QueuingTraceWriter(
                    new FixedFileWriterManger("logs\\trace.log", 1024*1024*4), new DefaultTraceEventFormatter(), new ZippingLogArchiver(10, 1024*1024*40), new DefaultThreadFactory()) )
                );

        //public QueuingTraceWriter(string fileName, long maxSize, int maxFiles, bool zip, ITraceEventFormatter formatter = null)
        //    : this(new WriterManger(fileName, maxSize), formatter, (zip ? (ILogArchiver)new ZippingLogArchiver(maxFiles, maxSize * 10) : new DeletingLogArchiver(maxFiles)), new DefaultThreadFactory())
        //{
        //}

            _logger = new HighPrecisionLoggerBuilder()
                .AddCollector(new TraceEventCollector(new ConsoleWriter()))
                .AddCollector(new TraceEventCollector(writer = new QueuingTraceWriter("logs\\trace.log", 1024*1024*4, 10, true, new DefaultTraceEventFormatter())))
                .AddProvider("random", new RandomProvider())
                .Build();

            Task[] tasks = Enumerable.Range(0, 10).Select(async i => await SplitTask(6, 6, i.ToString()).ConfigureAwait(false)).ToArray();
            Task.WaitAll(tasks);
            collector.Collect(new TraceEvent("DONE", DateTime.Now, "", new CustomData[0], new JObject())).Wait();
            Console.WriteLine("DONE");


            Console.ReadKey();

            writer.Dispose();
        }

        static async Task SplitTask(int depth, int width, string msg)
        {
            await Task.Delay(rnd.Next(50, 300)).ConfigureAwait(false);
            if (depth > 0)
            {
                using (IPerformanceTracker scope = _logger.Track("Foobar", new { Name = msg }, true))
                {
                    await Task.WhenAll(Enumerable.Range(0, width)
                            .Select(async i => await SplitTask(depth - 1, width, $"{msg}.{i}").ConfigureAwait(false)))
                        .ConfigureAwait(false);
                }
            }

            if (depth == 3)
            {
                using (IPerformanceTracker scope = _logger.Track("Foobar", new { Name = msg }, new CorrelationScope(true)))
                {
                    await Task.WhenAll(Enumerable.Range(0, width)
                            .Select(async i => await SplitTask(depth - 1, 3, $"{msg}.{i}").ConfigureAwait(false)))
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
