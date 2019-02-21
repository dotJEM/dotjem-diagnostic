using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.AdvParsers;
using ThreadState = System.Threading.ThreadState;

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
        private readonly Thread workerThread;
        private bool started = false;

        public NonLockingQueuingTraceWriter(string fileName, long maxSize, int maxFiles, bool zip, ITraceEventFormatter formatter = null)
        : this(new WriterManger(fileName, maxSize), formatter, (zip ? (ILogArchiver)new ZippingLogArchiver(maxFiles) : new DeletingLogArchiver(maxFiles)))
        {
        }

        public NonLockingQueuingTraceWriter(IWriterManger writerManager, ITraceEventFormatter formatter = null, ILogArchiver archiver = null)
        {
            this.writerManager = writerManager;
            this.archiver = archiver;
            this.formatter = formatter ?? new DefaultTraceEventFormatter();
            this.workerThread = new Thread(SyncWriteLoop);
        }

        public Task Write(TraceEvent trace)
        {
            if(Disposed)
                return Task.CompletedTask;

            eventsQueue.Enqueue(trace);
            //monitor.Pulse();
            lock (padlock)Monitor.PulseAll(padlock);
            EnsureWriteLoop();

            return Task.CompletedTask;
        }

        public Task AsyncFlush()
        {
            //while (eventsQueue.Count > 16)
            //{
            //    Debug.WriteLine("AsyncFlush::loop");
            //    await AsyncBufferedWrite().ConfigureAwait(false);
            //}

            //Debug.WriteLine("AsyncFlush::out");
            //await AsyncBufferedWrite().ConfigureAwait(false);
            //Debug.WriteLine("AsyncFlush::writerManager.Acquire().Flush().ConfigureAwait(false)");
            //await writerManager.Acquire().Flush().ConfigureAwait(false);

            //if (archiver.RollFile())
            //{

            //}
            return Task.CompletedTask;
        }

        private void Flush()
        {
            while (eventsQueue.Count > 16)
            {
                BufferedWrite();
            }
            BufferedWrite();
            writerManager.Acquire().Flush();
        }

        private void BufferedWrite()
        {
            int count = Math.Min(eventsQueue.Count, 64);
            if(count < 1)
                return;

            string[] lines = new string[count];
            for (int i = 0; i < count; i++)
                lines[i] = formatter.Format(eventsQueue.Dequeue());

            //TODO: This looks a bit silly, from this perspective it would be nice if the ITextWriter would just replace it's
            //      internal writer as needed underneath without us having to care about it here.
            //      This would also mean we could Acquire the writer, run the write loop and finally flush when the Queue is empty.
            ITextWriter writer = writerManager.Acquire();
            writer.WriteLines(lines);
        }

        private void EnsureWriteLoop()
        {
            if (started)
                return;

            lock (padlock)
            {
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
                        if (eventsQueue.Count < 1)
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
        void WriteLine(string value);
        void WriteLines(params string[] values);
        void Flush();
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

        public void WriteLine(string value)
        {
            //Note: This is significantly faster than having to refresh the file each time.
            //      As an added bonus, we don't have to flush explicitly each time to get the
            //      size which only saves even more time. That doesn't exclude us from
            //      explicitly flushing in other scenarios to ensure that data is written to the disk though.
     
            Size += writer.Encoding.GetByteCount(value) + newLineByteCount;
            writer.WriteLine(value);
        }

        public void Write(string value)
        {
            Size += writer.Encoding.GetByteCount(value);
            writer.Write(value);
        }

        public void WriteLines(params string[] values)
        {
            if (values.Length < 1)
                return;
            WriteLine(string.Join(writer.NewLine, values));
        }

        public void Flush() => writer.Flush();
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

        public Task<ITextWriter> TryOpenWithRetries(string path)
            => TryOpenWithRetries(path, 100, CancellationToken.None);

        public Task<ITextWriter> TryOpenWithRetries(string path, int maxTries)
            => TryOpenWithRetries(path, maxTries, CancellationToken.None);

        public Task<ITextWriter> TryOpenWithRetries(string path, CancellationToken cancellation)
            => TryOpenWithRetries(path, 100, cancellation);

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

    /// <summary>
    /// NOTE: This is meant for async -> sync integration, it's recomended to elevtate async patterns all the way, but
    ///       this is not always possible during refactoring of old code bases. This is also why these are not added as convinient extension methods.
    /// </summary>
    public static class Sync
    {
        public static T Await<T>(Task<T> task)
        {
            using (new NoSynchronizationContext())
            {
                try
                {
                    return task.Result;
                    //return Task.Run(() => task).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (AggregateException ex)
                {
                    ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                    // ReSharper disable HeuristicUnreachableCode
                    // The compiler requires either a throw or return, so even though this is unreachable, the compiler won't build unless it is there.
                    throw;
                    // ReSharper restore HeuristicUnreachableCode
                }
            }
        }

        public static T[] Await<T>(IEnumerable<Task<T>> tasks)
        {
            using (new NoSynchronizationContext())
            {
                try
                {
                    return Task.WhenAll(tasks).Result;
                    //return Task.Run(() => Task.WhenAll(tasks)).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (AggregateException ex)
                {
                    ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                    // ReSharper disable HeuristicUnreachableCode
                    // The compiler requires either a throw or return, so even though this is unreachable, the compiler won't build unless it is there.
                    throw;
                    // ReSharper restore HeuristicUnreachableCode
                }
            }
        }

        public static T[] Await<T>(params Task<T>[] tasks) => Await((IEnumerable<Task<T>>)tasks);

        public static void Await(Task task)
        {
            using (new NoSynchronizationContext())
            {
                try
                {
                    task.Wait();
                    //Task.Run(() => task).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (AggregateException ex)
                {
                    ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                    // ReSharper disable HeuristicUnreachableCode
                    // The compiler requires either a throw or return, so even though this is unreachable, the compiler won't build unless it is there.
                    throw;
                    // ReSharper restore HeuristicUnreachableCode
                }
            }
        }

        public static void Await(IEnumerable<Task> tasks)
        {
            try
            {
                Task.WhenAll(tasks).Wait();
                //Task.Run(() => Task.WhenAll(tasks)).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                // ReSharper disable HeuristicUnreachableCode
                // The compiler requires either a throw or return, so even though this is unreachable, the compiler won't build unless it is there.
                throw;
                // ReSharper restore HeuristicUnreachableCode
            }
        }

        public static void Await(params Task[] tasks) => Await((IEnumerable<Task>)tasks);

        private class NoSynchronizationContext : IDisposable
        {
            private readonly SynchronizationContext context;

            public NoSynchronizationContext()
            {
                context = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(null);
            }
            public void Dispose() =>
                SynchronizationContext.SetSynchronizationContext(context);
        }
    }
}