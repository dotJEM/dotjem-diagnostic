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
                new TraceEventCollector(writer = new QueuingTraceWriter("logs\\trace.log", 12000, 5, true, new DefaultTraceEventFormatter()))
                );

            _logger = new HighPrecisionLoggerBuilder(collector)
                .AddProvider("random", new RandomProvider())
                .Build();

            Task[] tasks = Enumerable.Range(0, 5).Select(async i => await SplitTask(3, i.ToString()).ConfigureAwait(false)).ToArray();
            Task.WaitAll(tasks);
            collector.Collect(new TraceEvent("DONE", DateTime.Now, "", new CustomData[0], new JObject())).Wait();
            Console.WriteLine("DONE");


            Console.ReadKey();

            writer.Dispose();
        }

        static async Task SplitTask(int depth, string msg)
        {
            await Task.Delay(rnd.Next(50, 300)).ConfigureAwait(false);
            if (depth > 0)
            {
                using (IPerformanceTracker scope = _logger.Track("Foobar", new { Name = msg }))
                {
                    await Task.WhenAll(Enumerable.Range(0, 10)
                            .Select(async i => await SplitTask(depth - 1, $"{msg}.{i}").ConfigureAwait(false)))
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
