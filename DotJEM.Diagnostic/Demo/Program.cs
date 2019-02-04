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
using DotJEM.Diagnostic.Writers;

namespace Demo
{
    class Program
    {
        private class RandomProvider : ICustomDataProvider
        {
            public object Data => rnd.Next();
            public string Format => "D9";
        }
        private static Random rnd = new Random();
        protected internal const int ITTERATIONS = 1000000 /4;
        private static ILogger _logger;

        static void Main(string[] args)
        {
            Directory.CreateDirectory("logs");
            var collector = new CompositeTraceEventCollector(
                new TraceEventCollector(new ConsoleWriter()),
                new TraceEventCollector(new NonLockingQueuingTraceWriter("logs\\trace.log", 12000, 5, true, new DefaultTraceEventFormatter())));

            _logger = new HighPrecisionLoggerBuilder(collector)
                .AddProvider("random", new RandomProvider())
                .Build();

            Task.WaitAll(
                Enumerable.Range(0, 5).Select(i => SplitTask(3, i.ToString())).ToArray()
            );
            Console.ReadKey();
            //SplitTask(3, "0").Wait();
        }

        static async Task SplitTask(int depth, string msg)
        {
            await Task.Delay(rnd.Next(50, 300));
            if (depth > 0)
            {
                using (new CorrelationScope())
                {
                    //_logger.Log("foo", new { Name = msg });
                    using (IPerformanceTracker tracker = _logger.Track("Foobar", new {Name = msg}))
                    {
                        //Console.WriteLine($"Running {msg} - {CorrelationScope.Current?.Value}");
                        await Task.WhenAll(Enumerable.Range(0, 10)
                                .Select(async i => await SplitTask(depth - 1, $"{msg}.{i}").ConfigureAwait(false)))
                            .ConfigureAwait(false);
                        //tracker.Commit();
                    }
                }
            }
        }

        static void SplitThread()
        {

        }

        static void Bench(string name, Action action)
        {
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < ITTERATIONS; i++)
                action();

            Console.WriteLine($"{name}: {w.ElapsedMilliseconds}");
        }
    }

    public static class Hashing
    {
        public static string ComputeId(this HashAlgorithm self, byte[] bytes)
        {
            byte[] hash = self.ComputeHash(bytes);
            return string.Join(string.Empty, Array.ConvertAll(hash, b => b.ToString("x2")));
        }
    }
}
