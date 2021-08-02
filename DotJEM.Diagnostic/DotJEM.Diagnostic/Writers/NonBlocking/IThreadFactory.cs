using System;
using System.Threading;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    public interface IThreadFactory
    {
        IWorkerThread Create(Action loop);
    }

    public interface IWorkerThread : IDisposable
    {
        void Start();
    }
}