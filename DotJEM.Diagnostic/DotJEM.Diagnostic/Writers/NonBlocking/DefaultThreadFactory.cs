using System;
using System.Threading;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    public class DefaultThreadFactory : IThreadFactory
    {
        public IWorkerThread Create(Action loop) => new WorkerThread(loop, true);
    }

    public class WorkerThread : IWorkerThread
    {
        private long started = 1;
        private readonly Thread workerThread;

        public WorkerThread(Action threadStart, bool isBackground)
        {
            this.workerThread = new Thread(new ThreadStart(threadStart));
            this.workerThread.IsBackground = isBackground;
        }

        public void Dispose()
        {
            if(Interlocked.Read(ref started) == 0) return;
            if(Interlocked.Exchange(ref started, 0) == 1) return;

            workerThread.Abort();
            workerThread.Join();
        }

        public void Start()
        {
            if(Interlocked.Read(ref started) == 1) return;
            if(Interlocked.Exchange(ref started, 1) == 1) return;
            workerThread.Start();
        }
    }
}