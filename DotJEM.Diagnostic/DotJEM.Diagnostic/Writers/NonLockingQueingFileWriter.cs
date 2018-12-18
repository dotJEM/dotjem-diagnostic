using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.Diagnostic.Writers
{
    public interface ITraceWriter : IDisposable
    {
        void Write(TraceEvent trace);
        Task Flush();
    }

    public class NonLockingQueuingTraceWriter : Disposable, ITraceWriter
    {
        private readonly Queue<TraceEvent> eventsQueue = new Queue<TraceEvent>();
        private readonly IWriterManger writerManager;
        private readonly ITraceEventFormatter formatter;
        private readonly ILogArchiver archiver;

        // Should be part of the Archiver service, which instead should be a DeletingArchiver or ZippingArchiver,
        // Archivers should be composeable.
        private long maxSize;
        private int maxFiles;

        private bool zip;


        private object padlock;
        private readonly Thread thread;

        public NonLockingQueuingTraceWriter(string fileName, long maxSize, int maxFiles, bool zip, ITraceEventFormatter formatter = null)
        : this(new WriterManger(fileName), formatter, (zip ? (ILogArchiver)new ZippingLogArchiver(maxFiles, maxSize) : new DeletingLogArchiver(maxFiles, maxSize)))
        {

        }

        public NonLockingQueuingTraceWriter(
            IWriterManger writerManager, ITraceEventFormatter formatter = null, ILogArchiver archiver = null)
        {
            if (maxSize != 0 && maxSize < 1024 * 16) throw new ArgumentOutOfRangeException(nameof(maxSize));
            if (maxFiles < 0) throw new ArgumentOutOfRangeException(nameof(maxFiles));

            this.writerManager = writerManager;
            this.archiver = archiver;
            this.formatter = formatter ?? new DefaultTraceEventFormatter();

            thread = new Thread(WriteLoop);
            thread.Start();
        }

        public void Write(TraceEvent trace)
        {
            if(Disposed)
                return;

            lock (padlock)
            {
                eventsQueue.Enqueue(trace);
                Monitor.PulseAll(padlock);
            }
        }

        public async Task Flush()
        {
            //todo using(TraceWriterLock writer = writerManager.Acquire())
            TextWriter writer = writerManager.Acquire(maxSize);
            while (eventsQueue.Count > 0)
            {
                await writer.WriteLineAsync(formatter.Format(eventsQueue.Dequeue()));
            }
            //if (archiver.RollFile())
            //{

            //}
        }

        private void WriteLoop()
        {
            try
            {
                lock (padlock)
                {
                    while (true)
                    {
                        if (eventsQueue.Count < 1)
                            Monitor.Wait(padlock);
                        Flush().Wait();
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Flush().Wait();
            }
            catch (Exception)
            {
                //TODO: Ignore for now, but we need an idea of how to deal with this.
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            thread.Abort();
            thread.Join();
        }
    }

    public interface ILogArchiver
    {

    }

    public class DeletingLogArchiver : ILogArchiver
    {
        public DeletingLogArchiver(int maxFiles, long maxSize)
        {

        }

    }
    public class ZippingLogArchiver : ILogArchiver {
        public ZippingLogArchiver(int maxFiles, long maxSize)
        {
            
        }
    }

    public interface ITraceEventFormatter
    {
        string Format(TraceEvent evt);
    }

    public class DefaultTraceEventFormatter : ITraceEventFormatter
    {
        public string Format(TraceEvent evt) => evt.ToString();
    }

    public interface IWriterManger
    {
        TextWriter Acquire(long maxSize);
    }

    public class WriterManger : IWriterManger
    {
        private readonly IWriterFactory writerFactory;
        private readonly IFileNameProvider fileNameProvider;

        private TextWriter currentWriter;
        private FileInfo currentFile;

        public WriterManger(string fileName) : this(new FileNameProvider(fileName), new StreamWriterFactory()) { }

        public WriterManger(IFileNameProvider fileNameProvider, IWriterFactory writerFactory)
        {
            this.fileNameProvider = fileNameProvider;
            this.writerFactory = writerFactory;
        }

        public TextWriter Acquire(long maxSize)
        {
            currentFile.Refresh();
            if (currentFile.Length <= maxSize)
                return currentWriter;

            currentWriter?.Close();

            return currentWriter = SafeOpen();
        }

        private TextWriter SafeOpen()
        {
            int count = 0;
            while (true)
            {
                if (writerFactory.TryOpen(currentFile.FullName, out TextWriter writer))
                    return writer;

                if (count > 10)
                    Thread.Sleep(count * 10);

                currentFile = new FileInfo(fileNameProvider.Id(count));
                count++;
            }

        }
    }


    public interface IWriterFactory
    {
        bool TryOpen(string path, out TextWriter writer);
        Task<TextWriter> TryOpenWithRetries(string path);
        Task<TextWriter> TryOpenWithRetries(string path, int maxTries);
        Task<TextWriter> TryOpenWithRetries(string path, CancellationToken cancellation);
        Task<TextWriter> TryOpenWithRetries(string path, int maxTries, CancellationToken cancellation);
    }

    public class StreamWriterFactory : IWriterFactory
    {
        public bool TryOpen(string path, out TextWriter writer)
        {
            try
            {
                writer = new StreamWriter(path, true);
                return true;
            }
            catch
            {
                writer = null;
                return false;
            }
        }

        public async Task<TextWriter> TryOpenWithRetries(string path)
            => await TryOpenWithRetries(path, 100, CancellationToken.None);

        public async Task<TextWriter> TryOpenWithRetries(string path, int maxTries)
            => await TryOpenWithRetries(path, maxTries, CancellationToken.None);

        public async Task<TextWriter> TryOpenWithRetries(string path, CancellationToken cancellation)
            => await TryOpenWithRetries(path, 100, cancellation);

        public async Task<TextWriter> TryOpenWithRetries(string path, int maxTries, CancellationToken cancellation)
        {
            for (int i = 0; i < maxTries; i++)
            {
                if (cancellation.IsCancellationRequested)
                    return null;

                if (TryOpen(path, out TextWriter writer))
                    return writer;

                if (i > maxTries / 10)
                    await Task.Delay(i * 10, cancellation);
            }
            return null;
        }
    }
}
