using System;
using System.Linq;
using System.Security.Cryptography;

namespace DotJEM.Diagnostic.Common
{
    public class IdProvider : Disposable
    {
        public static IdProvider Default { get; }= new IdProvider();

        private readonly int length = 8;
        private readonly SHA256 hasher = SHA256.Create();

        public string Next => Compute();

        public string Compute() => Compute(Guid.NewGuid());

        public string Compute(Guid guid)
        {
            byte[] hash = hasher.ComputeHash(guid.ToByteArray());
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
}