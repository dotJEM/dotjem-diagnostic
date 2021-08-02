using System;
using System.Threading;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    public class NonBackgroundThreadThreadFactory : IThreadFactory
    {
        public IWorkerThread Create(Action loop) => new WorkerThread(loop, false);
    }
}