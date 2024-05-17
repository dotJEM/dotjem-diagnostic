using System;
using System.Linq;
using System.Security.Cryptography;

namespace DotJEM.Diagnostic.Common
{
    public class IdProvider : Disposable
    {
        public static IdProvider Default { get; } = new IdProvider();

        private readonly int length = 8;
        private readonly RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();


        public string Next => Compute();

        public string Compute()
        {
            byte[] idBytes = new byte[8];
            random.GetBytes(idBytes);
            string value = string.Join(string.Empty, idBytes.Select(b => b.ToString("x2")));
            return length < 1 ? value : value.Substring(0, length);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                random.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}