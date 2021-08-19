using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DotJEM.AdvParsers;
using DotJEM.Diagnostic.Writers.Output;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    /// <summary>
    /// Implementation of the <see cref="IWriterManger"/> interface which will always use a unique file name when opening a new writer.
    /// </summary>
    /// <remarks>
    /// The default writer manager will always ensure a unique file name everytime a new log file is opened for logging.
    /// This also means that this manager will write to a new file across application shutdown and startup.
    /// </remarks>
    public class DefaultWriterManager : IWriterManger
    {
        private readonly long maxSizeInBytes;
        private readonly IWriterFactory writerFactory;
        private ITextWriter currentWriter;
        private FileInfo currentFile;
        
        public IFileNameProvider NameProvider { get; }

        public DefaultWriterManager(string fileName) : this(new FileNameProvider(fileName), new StreamWriterFactory(), 64.KiloBytes()) { }
        public DefaultWriterManager(string fileName, long maxSizeInBytes) : this(new FileNameProvider(fileName), new StreamWriterFactory(), maxSizeInBytes) { }

        public DefaultWriterManager(IFileNameProvider fileNameProvider, IWriterFactory writerFactory, long maxSizeInBytes)
        {
            if (maxSizeInBytes != 0 && maxSizeInBytes < 8.KiloBytes()) throw new ArgumentOutOfRangeException(nameof(maxSizeInBytes));
            
            this.NameProvider = fileNameProvider;
            this.writerFactory = writerFactory;
            this.maxSizeInBytes = maxSizeInBytes;
            this.currentFile = new FileInfo(fileNameProvider.Unique());

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
            
            currentWriter.Flush();
            currentWriter.Dispose();
            currentWriter = null;
            currentFile = new FileInfo(NameProvider.Unique());
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
                if (writerFactory.TryOpen(currentFile.FullName, out ITextWriter writer))
                    return writer;
                currentFile = new FileInfo(NameProvider.Unique());
                count++;
            }
        }
    }
}