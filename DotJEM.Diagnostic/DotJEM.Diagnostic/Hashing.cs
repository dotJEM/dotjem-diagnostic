using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace DotJEM.Diagnostic
{
    public class IdProvider : Disposable
    {
        public static IdProvider Default { get; }= new IdProvider();

        private readonly int length = 8;
        private readonly SHA256 hasher = SHA256.Create();

        public string Next => Compute();

        public string Compute()
        {
            byte[] hash = hasher.ComputeHash(Guid.NewGuid().ToByteArray());
            string value = string.Join(string.Empty, hash.Select(b => b.ToString("x2")));
            return length < 1 ? value : value.Substring(0, length);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                hasher.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class HighResolutionDateTime
    {
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        static extern void GetSystemTimePreciseAsFileTime(out long fileTime);

        public static DateTimeOffset Now
        {
            get
            {
                GetSystemTimePreciseAsFileTime(out var fileTime);
                return DateTimeOffset.FromFileTime(fileTime);
            }
        }
    }
}