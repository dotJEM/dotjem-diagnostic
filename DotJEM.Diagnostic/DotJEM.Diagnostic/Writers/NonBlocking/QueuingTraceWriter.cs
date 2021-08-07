using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Diagnostic.Common;
using DotJEM.Diagnostic.Model;
using DotJEM.Diagnostic.Writers.Formatter;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    /// <summary>
    /// Provides a <see cref="ITraceWriter"/> that doesn't block while waiting for the IO layer when writing out trace events.
    /// Instead it Queues each event on a Queue and pulses a writer thread.
    /// </summary>
    public class QueuingTraceWriter : Disposable, ITraceWriter<TraceEvent>
    {
        private readonly Queue<TraceEvent> eventsQueue = new Queue<TraceEvent>();
        private readonly IWriterManger writerManager;
        private readonly ITraceFormatter<TraceEvent> formatter;
        private readonly ILogArchiver archiver;

        // Should be part of the Archiver service, which instead should be a DeletingArchiver or ZippingArchiver,
        // Archivers should be composeable.

        // Have a look at: TODO: https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.itargetblock-1?view=netcore-2.2

        private readonly object padlock = new object();
        private readonly Thread workerThread;
        private readonly Queue<TaskCompletionSource<byte>> awaitingFlush = new Queue<TaskCompletionSource<byte>>();
        private bool started = false;

        public QueuingTraceWriter(string fileName, long maxSize, int maxFiles, bool zip, ITraceFormatter<TraceEvent> formatter = null)
            : this(new WriterManger(fileName, maxSize), formatter, (zip ? (ILogArchiver)new ZippingLogArchiver(maxFiles, maxSize * 10) : new DeletingLogArchiver(maxFiles)), new DefaultThreadFactory())
        {
        }

        public QueuingTraceWriter(IWriterManger writerManager, ITraceFormatter<TraceEvent> formatter = null, ILogArchiver archiver = null, IThreadFactory factory = null)
        {
            this.writerManager = writerManager;
            this.archiver = archiver;
            this.formatter = formatter ?? new DefaultTraceFormatter<TraceEvent>();
            this.workerThread = (factory ?? new DefaultThreadFactory()).Create(SyncWriteLoop);
        }

        public Task Write(TraceEvent trace)
        {
            if (trace == null) throw new ArgumentNullException(nameof(trace));
            if (Disposed)
                return Task.CompletedTask;

            lock (padlock)
            {
                eventsQueue.Enqueue(trace);
            }
            EnsureWriteLoop();
            Pulse();
            return Task.CompletedTask;
        }

        private void Pulse()
        {
            lock (padlock) Monitor.PulseAll(padlock);
        }

        public Task AsyncFlush()
        {
            TaskCompletionSource<byte> wait = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            awaitingFlush.Enqueue(wait);

            lock (padlock) Monitor.PulseAll(padlock);

            return wait.Task;
        }

        private void Flush()
        {
            bool replaced = false;
            while (eventsQueue.Count > 16)
            {
                replaced |= BufferedWrite();
            }
            replaced |= BufferedWrite();
            replaced |= writerManager.Flush();

            if (replaced) archiver.Archive(writerManager);

            while (awaitingFlush.Count > 0)
                awaitingFlush.Dequeue().TrySetResult(1);
        }

        private bool BufferedWrite()
        {
            int count = Math.Min(eventsQueue.Count, 64);
            if (count < 1)
                return false;

            string[] lines = new string[count];
            for (int i = 0; i < count; i++)
            {
                lock (padlock)
                {
                    TraceEvent next = eventsQueue.Dequeue();
                    lines[i] = formatter.Format(next);
                }
            }

            return writerManager.WriteLines(lines);
        }

        private void EnsureWriteLoop()
        {
            if (started)
                return;

            lock (padlock)
            {
                if (started)
                    return;

                started = true;
                workerThread.Start();
            }
        }

        private void SyncWriteLoop()
        {
            while (true)
            {
                try
                {
                    lock (padlock)
                    {
                        while (eventsQueue.Count < 1)
                            Monitor.Wait(padlock);

                        Flush();

                        if (Disposed)
                            break;
                    }
                }
                catch (ThreadAbortException)
                {
                    Flush();
                }
                catch (Exception)
                {
                    //TODO: Ignore for now, but we need an idea of how to deal with this.
                }
            }
            Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            workerThread.Abort();
            workerThread.Join();
        }
    }
}