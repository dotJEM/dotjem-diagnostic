using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.AdvParsers;

namespace DotJEM.Diagnostic.Writers
{
    public class NonLockingQueuingTraceWriter : Disposable, ITraceWriter
    {
        private readonly Queue<TraceEvent> eventsQueue = new Queue<TraceEvent>();
        private readonly IWriterManger writerManager;
        private readonly ITraceEventFormatter formatter;
        private readonly ILogArchiver archiver;

        // Should be part of the Archiver service, which instead should be a DeletingArchiver or ZippingArchiver,
        // Archivers should be composeable.

        private readonly object padlock = new object();
        private readonly Thread thread;

        public NonLockingQueuingTraceWriter(string fileName, long maxSize, int maxFiles, bool zip, ITraceEventFormatter formatter = null)
        : this(new WriterManger(fileName, maxSize), formatter, (zip ? (ILogArchiver)new ZippingLogArchiver(maxFiles) : new DeletingLogArchiver(maxFiles)))
        {
        }

        public NonLockingQueuingTraceWriter(IWriterManger writerManager, ITraceEventFormatter formatter = null, ILogArchiver archiver = null)
        {
            this.writerManager = writerManager;
            this.archiver = archiver;
            this.formatter = formatter ?? new DefaultTraceEventFormatter();

            thread = new Thread(WriteLoop);
            thread.Start();
        }

        public async Task Write(TraceEvent trace)
        {
            if(Disposed)
                return;

            lock (padlock)
            {
                eventsQueue.Enqueue(trace);
                Monitor.PulseAll(padlock);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

        public async Task Flush()
        {
            while (eventsQueue.Count > 16)
                await BufferedWrite().ConfigureAwait(false);
            await BufferedWrite().ConfigureAwait(false);
            await writerManager.Acquire().FlushAsync().ConfigureAwait(false);

            //if (archiver.RollFile())
            //{

            //}
        }

        private async Task BufferedWrite()
        {
            int count = Math.Min(eventsQueue.Count, 64);
            string[] lines = new string[count];
            for (int i = 0; i < count; i++)
                lines[i] = formatter.Format(eventsQueue.Dequeue());

            //TODO: This looks a bit silly, from this perspective it would be nice if the ITextWriter would just replace it's
            //      internal writer as needed underneath without us having to care about it here.
            //      This would also mean we could Acquire the writer, run the write loop and finally flush when the Queue is empty.
            ITextWriter writer = writerManager.Acquire();
            await writer.WriteLinesAsync(lines).ConfigureAwait(false);
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
                        try
                        {
                            Flush().Wait();
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Flush().Wait();
            }
            catch (Exception ex)
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
        public DeletingLogArchiver(int maxFiles)
        {
            if (maxFiles < 0) throw new ArgumentOutOfRangeException(nameof(maxFiles));
        }

    }
    public class ZippingLogArchiver : ILogArchiver {
        public ZippingLogArchiver(int maxFiles)
        {
            if (maxFiles < 0) throw new ArgumentOutOfRangeException(nameof(maxFiles));
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
        ITextWriter Acquire();
    }

    public interface ITextWriter
    {
        long Size { get; }
        void Close();
        Task WriteLineAsync(string value);
        Task WriteLinesAsync(params string[] values);
        Task FlushAsync();
    }

    public class TextWriterProxy : ITextWriter
    {
        private readonly TextWriter writer;
        private readonly int newLineByteCount;

        public long Size { get; private set; }

        public TextWriterProxy(TextWriter current, long currentSize)
        {
            Size = currentSize;
            writer = current;
            newLineByteCount = writer.Encoding.GetByteCount(writer.NewLine);
        }

        public async Task WriteLineAsync(string value)
        {
            //Note: This is significantly faster than having to refresh the file each time.
            //      As an added bonus, we don't have to flush explicitly each time to get the
            //      size which only saves even more time. That doesn't exclude us from
            //      explicitly flushing in other scenarios to ensure that data is written to the disk though.
     
            Size += writer.Encoding.GetByteCount(value) + newLineByteCount;
            await writer.WriteLineAsync(value).ConfigureAwait(false);
        }

        public async Task WriteLinesAsync(params string[] values)
        {
            await WriteLineAsync(string.Join(writer.NewLine, values)).ConfigureAwait(false);
        }

        public async Task FlushAsync() => await writer.FlushAsync().ConfigureAwait(false);
        public void Close() => writer.Close();
    }

    public class WriterManger : IWriterManger
    {
        private readonly long maxSizeInBytes;
        private readonly IWriterFactory writerFactory;
        private readonly IFileNameProvider fileNameProvider;

        private ITextWriter currentWriter;
        private FileInfo currentFile;

        public WriterManger(string fileName) : this(new FileNameProvider(fileName), new StreamWriterFactory(), 64.KiloBytes()) { }
        public WriterManger(string fileName, long maxSizeInBytes) : this(new FileNameProvider(fileName), new StreamWriterFactory(), maxSizeInBytes) { }

        public WriterManger(IFileNameProvider fileNameProvider, IWriterFactory writerFactory, long maxSizeInBytes)
        {
            if (maxSizeInBytes != 0 && maxSizeInBytes < 8.KiloBytes()) throw new ArgumentOutOfRangeException(nameof(maxSizeInBytes));

            this.fileNameProvider = fileNameProvider;
            this.writerFactory = writerFactory;
            this.maxSizeInBytes = maxSizeInBytes;

            this.currentFile = new FileInfo(fileNameProvider.FullName);
        }

        public ITextWriter Acquire()
        {
            if (currentWriter?.Size <= maxSizeInBytes)
                return currentWriter;

            currentWriter?.Close();
            return currentWriter = SafeOpen();
        }

        private ITextWriter SafeOpen()
        {
            int count = 0;
            while (true)
            {
                if (writerFactory.TryOpen(currentFile.FullName, out ITextWriter writer))
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
        bool TryOpen(string path, out ITextWriter writer);
        Task<ITextWriter> TryOpenWithRetries(string path);
        Task<ITextWriter> TryOpenWithRetries(string path, int maxTries);
        Task<ITextWriter> TryOpenWithRetries(string path, CancellationToken cancellation);
        Task<ITextWriter> TryOpenWithRetries(string path, int maxTries, CancellationToken cancellation);
    }

    public class StreamWriterFactory : IWriterFactory
    {
        public bool TryOpen(string path, out ITextWriter writer)
        {
            try
            {
                FileInfo file = new FileInfo(path);
                StreamWriter streamWriter = new StreamWriter(path, true);
                writer = new TextWriterProxy(streamWriter, file.Length);
                return true;
            }
            catch
            {
                writer = null;
                return false;
            }
        }

        public async Task<ITextWriter> TryOpenWithRetries(string path)
            => await TryOpenWithRetries(path, 100, CancellationToken.None).ConfigureAwait(false);

        public async Task<ITextWriter> TryOpenWithRetries(string path, int maxTries)
            => await TryOpenWithRetries(path, maxTries, CancellationToken.None).ConfigureAwait(false);

        public async Task<ITextWriter> TryOpenWithRetries(string path, CancellationToken cancellation)
            => await TryOpenWithRetries(path, 100, cancellation).ConfigureAwait(false);

        public async Task<ITextWriter> TryOpenWithRetries(string path, int maxTries, CancellationToken cancellation)
        {
            for (int i = 0; i < maxTries; i++)
            {
                if (cancellation.IsCancellationRequested)
                    return null;

                if (TryOpen(path, out ITextWriter writer))
                    return writer;

                if (i > maxTries / 10)
                    await Task.Delay(i * 10, cancellation).ConfigureAwait(false);
            }
            return null;
        }
    }
}
