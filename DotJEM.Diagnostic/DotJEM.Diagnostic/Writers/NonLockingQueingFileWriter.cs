using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.AdvParsers;
using DotJEM.Diagnostic.Common;
using DotJEM.Diagnostic.Model;
using DotJEM.Diagnostic.Writers.Output;

namespace DotJEM.Diagnostic.Writers
{
    public interface IThreadFactory
    {
        Thread Create(ThreadStart loop);
    }

    public class DefaultThreadFactory : IThreadFactory
    {
        public Thread Create(ThreadStart loop) => new Thread(loop) { IsBackground = true };
    }

    public class NonBackgroundThreadThreadFactory : IThreadFactory
    {
        public Thread Create(ThreadStart loop) => new Thread(loop) { IsBackground = false };
    }

    /// <summary>
    /// Provides a <see cref="ITraceWriter"/> that doesn't block while waiting for the IO layer when writing out trace events.
    /// Instead it Queues each event on a Queue and pulses a writer thread.
    /// </summary>
    public class QueuingTraceWriter : Disposable, ITraceWriter
    {
        private readonly Queue<TraceEvent> eventsQueue = new Queue<TraceEvent>();
        private readonly IWriterManger writerManager;
        private readonly ITraceEventFormatter formatter;
        private readonly ILogArchiver archiver;

        // Should be part of the Archiver service, which instead should be a DeletingArchiver or ZippingArchiver,
        // Archivers should be composeable.

        // Have a look at: TODO: https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.itargetblock-1?view=netcore-2.2

        private readonly object padlock = new object();
        private readonly Thread workerThread;
        private readonly Queue<TaskCompletionSource<byte>> awaitingFlush = new Queue<TaskCompletionSource<byte>>();
        private bool started = false;

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

        public Task Write(TraceEvent trace)
        {
            if (Disposed)
                return Task.CompletedTask;

            eventsQueue.Enqueue(trace);
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
                lines[i] = formatter.Format(eventsQueue.Dequeue());

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
        void Archive(IWriterManger manager);
    }

    public class DeletingLogArchiver : ILogArchiver
    {
        private readonly int maxFiles;

        public DeletingLogArchiver(int maxFiles)
        {
            if (maxFiles < 0) throw new ArgumentOutOfRangeException(nameof(maxFiles));
            this.maxFiles = maxFiles;
        }

        public void Archive(IWriterManger files)
        {
            List<FileInfo> listOfFiles = files
                .AllFiles()
                .ToList();

            if (listOfFiles.Count < maxFiles)
                return;

            foreach (FileInfo fileInfo in listOfFiles.OrderByDescending(file => file.CreationTime).Skip(maxFiles))
                fileInfo.Delete();
        }
    }
    public class ZippingLogArchiver : ILogArchiver
    {
        private readonly int maxFiles;
        private readonly long maxSize;

        public ZippingLogArchiver(int maxFiles, long maxSize)
        {
            if (maxFiles < 0) throw new ArgumentOutOfRangeException(nameof(maxFiles));
            this.maxFiles = maxFiles;
            this.maxSize = maxSize;
        }

        public void Archive(IWriterManger files)
        {
            List<FileInfo> listOfFiles = files
                .AllFiles()
                .ToList();

            if (listOfFiles.Count < maxFiles)
                return;

            using (ZipArchive archive = ZipFile.Open(files.NameProvider.Unique(".zip"), ZipArchiveMode.Create))
            {
                foreach (FileInfo file in listOfFiles)
                {
                    try
                    {
                        archive.CreateEntryFromFile(file.FullName, file.Name);
                        file.Delete();
                    }
                    catch (Exception e)
                    {
                    }
                }
            }

            List<FileInfo> listOfZipFiles = files
                .AllFiles(".zip")
                .ToList();

            if (listOfZipFiles.Count < maxFiles)
                return;


            //TODO: Try catch correctly
            using (ZipArchive targetArchive = ZipFile.Open(files.NameProvider.Unique(".zip"), ZipArchiveMode.Create))
            {
                foreach (FileInfo zipFile in listOfZipFiles.Where(file => file.Length < maxSize))
                {
                    try
                    {
                        using (ZipArchive sourceArchive = ZipFile.OpenRead(zipFile.FullName))
                        {
                            foreach (ZipArchiveEntry sourceEntry in sourceArchive.Entries)
                            {
                                ZipArchiveEntry targetEntry = targetArchive.CreateEntry(sourceEntry.Name);
                                using (Stream sourceStream = sourceEntry.Open())
                                {
                                    using (Stream targetStream = targetEntry.Open())
                                    {
                                        sourceStream.CopyTo(targetStream);
                                    }
                                }
                            }
                        }
                        zipFile.Delete();
                    }
                    catch (Exception e)
                    {
                    }
                }
            }

            listOfZipFiles = files
                .AllFiles(".zip")
                .ToList();

            if (listOfZipFiles.Count < maxFiles)
                return;

            foreach (FileInfo fileInfo in listOfZipFiles.OrderByDescending(file => file.CreationTime).Skip(maxFiles))
                fileInfo.Delete();
        }
    }

    public static class WriterManagerExtensions
    {
        public static bool Close(this IWriterManger self)
        {
            self.Acquire(out bool replaced).Close();
            return replaced;
        }

        public static bool WriteLine(this IWriterManger self, string value)
        {
            self.Acquire(out bool replaced).WriteLine(value);
            return replaced;
        }

        public static bool WriteLines(this IWriterManger self, params string[] values)
        {
            self.Acquire(out bool replaced).WriteLines(values);
            return replaced;
        }

        public static bool Flush(this IWriterManger self)
        {
            self.Acquire(out bool replaced).Flush();
            return replaced;
        }
    }

    public interface IFileLister
    {
        IEnumerable<FileInfo> AllFiles(string extension = null);
    }

    public interface IWriterManger : IFileLister
    {
        IFileNameProvider NameProvider { get; }
        ITextWriter Acquire();
        ITextWriter Acquire(out bool replaced);
    }

    public class WriterManger : IWriterManger
    {
        private readonly long maxSizeInBytes;
        private readonly IWriterFactory writerFactory;
        private ITextWriter currentWriter;
        private FileInfo currentFile;

        public IFileNameProvider NameProvider { get; }

        public WriterManger(string fileName) : this(new FileNameProvider(fileName), new StreamWriterFactory(), 64.KiloBytes()) { }
        public WriterManger(string fileName, long maxSizeInBytes) : this(new FileNameProvider(fileName), new StreamWriterFactory(), maxSizeInBytes) { }

        public WriterManger(IFileNameProvider fileNameProvider, IWriterFactory writerFactory, long maxSizeInBytes)
        {
            if (maxSizeInBytes != 0 && maxSizeInBytes < 8.KiloBytes()) throw new ArgumentOutOfRangeException(nameof(maxSizeInBytes));
            
            this.NameProvider = fileNameProvider;
            this.writerFactory = writerFactory;
            this.maxSizeInBytes = maxSizeInBytes;
            this.currentFile = new FileInfo(fileNameProvider.FullName);

            Directory.CreateDirectory(fileNameProvider.Directory);
        }

        public ITextWriter Acquire() => Acquire(out _);

        public ITextWriter Acquire(out bool replaced)
        {
            replaced = false;
            if (currentWriter?.Size <= maxSizeInBytes)
                return currentWriter;

            replaced = true;

            if (currentWriter == null)
                return currentWriter = SafeOpen();
            
            currentWriter.Close();
            currentWriter.Dispose();
            currentWriter = null;
            currentFile.MoveTo(NameProvider.Unique());
            currentFile = new FileInfo(NameProvider.FullName);

            return currentWriter = SafeOpen();
        }

        public IEnumerable<FileInfo> AllFiles(string extension = null)
        {
            return NameProvider.AllFiles(extension).Where(file => !file.FullName.Equals(currentFile.FullName, StringComparison.OrdinalIgnoreCase));
        }

        private ITextWriter SafeOpen()
        {
            int count = 0;
            while (true)
            {
                if (writerFactory.TryOpenWithRetries(currentFile.FullName, 20, CancellationToken.None, out ITextWriter writer))
                    return writer;

                currentFile = new FileInfo(NameProvider.Id(count));
                count++;
            }
        }
    }
}