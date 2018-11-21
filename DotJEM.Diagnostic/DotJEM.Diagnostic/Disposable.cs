using System;

namespace DotJEM.Diagnostic
{
    public class Disposable : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            if (Disposed)
                return;

            Dispose(Disposed = true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}