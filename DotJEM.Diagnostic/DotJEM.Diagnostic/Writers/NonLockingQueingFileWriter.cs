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
        bool TryFlush();
    }

    public class NonLockingQueuingTraceWriter : Disposable, ITraceWriter
    {
        private readonly Queue<TraceEvent> eventsQueue = new Queue<TraceEvent>();
        private readonly IFileNameProvider nameProvider;
        private readonly IWriterFactory writerFactory;
        private readonly IWriterManger writerManager;
        private long maxSize;
        private int maxFiles;
        private bool zip;


        public NonLockingQueuingTraceWriter(string fileName, long maxSize, int maxFiles, bool zip)
        : this(,  maxSize, maxFiles, zip)
        {

        }

        public NonLockingQueuingTraceWriter(IWriterManger writerManager, long maxSize, int maxFiles, bool zip)
        {
            this.nameProvider = nameProvider;
            this.writerManager = writerManager;
            this.maxSize = maxSize;
            this.maxFiles = maxFiles;
            this.zip = zip;
        }

        public void Write(TraceEvent trace)
        {
            throw new NotImplementedException();
        }

        public bool TryFlush()
        {
            throw new NotImplementedException();
        }


    }

    public interface IWriterManger
    {
        TextWriter Acquire();
    }

    public class WriterManger : IWriterManger
    {
        private FileNameProvider fileNameProvider;
        private StreamWriterFactory streamWriterFactory;

        public WriterManger(string fileName) : this(new FileNameProvider(fileName), new StreamWriterFactory()) { }

        public WriterManger(FileNameProvider fileNameProvider, StreamWriterFactory streamWriterFactory)
        {
            this.fileNameProvider = fileNameProvider;
            this.streamWriterFactory = streamWriterFactory;
        }

        public TextWriter Acquire()
        {
            throw new NotImplementedException();
        }
    }


    public interface IWriterFactory
    {
        bool TryOpen(string path, out TextWriter writer);


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
