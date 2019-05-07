using System;

namespace DotJEM.Diagnostic.Common
{
    public class Disposable : IDisposable
    {
        protected bool Disposed { get; private set; }

        public void Dispose()
        {
            if (Disposed)
                return;

            Dispose(Disposed);
            Disposed = true;
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}