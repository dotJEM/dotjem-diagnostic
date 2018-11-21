using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Diagnostic;

namespace Demo
{
    class Program
    {
        private static Random rnd = new Random();
        protected internal const int ITTERATIONS = 1000000 /4;

        static void Main(string[] args)
        {
            SplitTask(3, "0").Wait();
        }

        


        static async Task SplitTask(int depth, string msg)
        {
            await Task.Delay(rnd.Next(50, 300));
            if (depth > 0)
            {
                using (new CorrelationScope())
                {
                    Console.WriteLine($"Running {msg} - {CorrelationScope.Current?.Value}");
                    await Task.WhenAll(Enumerable.Range(0, 10)
                        .Select(async i => await SplitTask(depth - 1, $"{msg}.{i}").ConfigureAwait(false)))
                        .ConfigureAwait(false);
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
