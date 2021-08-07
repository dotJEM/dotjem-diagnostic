using System;
using System.Linq;
using System.Security.Cryptography;

namespace DotJEM.Diagnostic.Common
{
    public interface IIdProvider : IDisposable
    {
        string Next { get; }
        string Compute();
        string Compute(Guid guid);
        byte[] NextBytes { get; }
        byte[] ComputeBytes();
        byte[] ComputeBytes(Guid guid);
    }

    public class IdProvider : Disposable, IIdProvider
    {
        public static IdProvider Default { get; }= new IdProvider();

        private readonly int length = 4;
        private readonly SHA256 hasher = SHA256.Create();

        public string Next => Compute();
        public byte[] NextBytes => ComputeBytes();

        public string Compute() => Compute(Guid.NewGuid());
        public byte[] ComputeBytes() => ComputeBytes(Guid.NewGuid());

        public string Compute(Guid guid)
        {
            byte[] hash = hasher.ComputeHash(guid.ToByteArray());
            string value = string.Join(string.Empty, hash.Select(b => b.ToString("x2")));
            return length < 1 ? value : value.Substring(0, length);
        }

        public byte[] ComputeBytes(Guid guid)
        {
            byte[] hash = hasher.ComputeHash(guid.ToByteArray());
            return hash.Take(length).ToArray();
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
}