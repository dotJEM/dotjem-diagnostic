using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Diagnostic;
using DotJEM.Diagnostic.Correlation;

namespace Demo
{
    class Program
    {
        private static Random rnd = new Random();
        protected internal const int ITTERATIONS = 1000000 /4;
        private static IPerformanceMonitor monitor;

        static void Main(string[] args)
        {
            monitor = new PerformanceMonitorBuilder()
                .Build();

            Task.WaitAll(
                Enumerable.Range(0, 5).Select(i => SplitTask(3, i.ToString())).ToArray()
            );

            //SplitTask(3, "0").Wait();
        }

        static async Task SplitTask(int depth, string msg)
        {
            await Task.Delay(rnd.Next(50, 300));
            if (depth > 0)
            {
                using (new CorrelationScope())
                {
                    //monitor.Trace("foo", new { Name = msg });
                    using (var tracker = monitor.Track("Foobar", new {Name = msg}))
                    {
                        //Console.WriteLine($"Running {msg} - {CorrelationScope.Current?.Value}");
                        await Task.WhenAll(Enumerable.Range(0, 10)
                                .Select(async i => await SplitTask(depth - 1, $"{msg}.{i}").ConfigureAwait(false)))
                            .ConfigureAwait(false);
                        tracker.Commit();
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
