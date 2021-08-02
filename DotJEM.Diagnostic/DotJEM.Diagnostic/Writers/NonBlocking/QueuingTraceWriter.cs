using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Diagnostic.Common;
using DotJEM.Diagnostic.Model;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    public class TraceQueue
    {
        private int count = 0;
        private readonly object padlock = new object();
        private readonly Queue<TraceEvent> events = new Queue<TraceEvent>();

        public bool HasItems => count > 0;

        public void Enqueue(TraceEvent trace)
        {
            lock (padlock)
            {
                events.Enqueue(trace);
                Interlocked.Increment(ref count);
                Monitor.Pulse(padlock);
            }
        }

        public TraceEvent[] Dequeue(int max)
        {
            lock (padlock)
            {
                while (count < 1) Monitor.Wait(padlock);

                int take = Math.Min(count, max);
                Interlocked.Add(ref count, -take);
                TraceEvent[] batch = new TraceEvent[take];
                for (int i = 0; i < take; i++)
                    batch[i] = events.Dequeue();
                return batch;
            }
        }
    }

    /// <summary>
    /// Provides a <see cref="ITraceWriter"/> that doesn't block while waiting for the IO layer when writing out trace events.
    /// Instead it Queues each event on a Queue and pulses a writer thread.
    /// </summary>
    public class QueuingTraceWriter : Disposable, ITraceWriter
    {
        private readonly TraceQueue eventsQueue = new TraceQueue();
        private readonly IWriterManger writerManager;
        private readonly ITraceEventFormatter formatter;
        private readonly ILogArchiver archiver;

        // Should be part of the Archiver service, which instead should be a DeletingArchiver or ZippingArchiver,
        // Archivers should be composeable.

        // Have a look at: TODO: https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.itargetblock-1?view=netcore-2.2

        private readonly Queue<TaskCompletionSource<byte>> awaitingFlush = new Queue<TaskCompletionSource<byte>>();
        private readonly IWorkerThread workerThread;

        public QueuingTraceWriter(string fileName, long maxSize, int maxFiles, bool zip, ITraceEventFormatter formatter = null)
            : this(new WriterManger(fileName, maxSize), formatter, (zip ? (ILogArchiver)new ZippingLogArchiver(maxFiles, maxSize * 10) : new DeletingLogArchiver(maxFiles)), new DefaultThreadFactory())
        {
        }

        public QueuingTraceWriter(IWriterManger writerManager, ITraceEventFormatter formatter = null, ILogArchiver archiver = null, IThreadFactory factory = null)
        {
            this.writerManager = writerManager;
            this.archiver = archiver;
            this.formatter = formatter ?? new DefaultTraceEventFormatter();
            this.workerThread = (factory ?? new DefaultThreadFactory()).Create(SyncWriteLoop);
        }

        public Task WriteAsync(TraceEvent trace)
        {
            if (trace == null) throw new ArgumentNullException(nameof(trace));
            if (Disposed)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                eventsQueue.Enqueue(trace);
                workerThread.Start();
            });
        }

        public Task FlushAsync()
        {
            TaskCompletionSource<byte> wait = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            awaitingFlush.Enqueue(wait);
            return wait.Task;
        }

        private void Flush()
        {
            while (eventsQueue.HasItems)
            {
                string[] lines = eventsQueue
                    .Dequeue(128)
                    .Select(formatter.Format)
                    .ToArray();

                if (writerManager.WriteLines(lines))
                    archiver.Archive(writerManager);

            }

            if (writerManager.Flush())
                archiver.Archive(writerManager);

            while (awaitingFlush.Count > 0)
                awaitingFlush.Dequeue().TrySetResult(1);
        }

        private void SyncWriteLoop()
        {
            while (true)
            {
                try
                {
                    Flush();
                    if (Disposed)
                        break;
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

            workerThread.Dispose();
        }
    }
}