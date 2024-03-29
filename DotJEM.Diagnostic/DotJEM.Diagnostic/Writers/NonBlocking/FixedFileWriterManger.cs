﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DotJEM.AdvParsers;
using DotJEM.Diagnostic.Writers.Output;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    /// <summary>
    /// Implementation of the <see cref="IWriterManger"/> interface which attempts to use the same fixed file name for the currently active log.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class FixedFileWriterManger : IWriterManger
    {
        private readonly long maxSizeInBytes;
        private readonly IWriterFactory writerFactory;
        private ITextWriter currentWriter;
        private FileInfo currentFile;

        public IFileNameProvider NameProvider { get; }

        public FixedFileWriterManger(string fileName) : this(new FileNameProvider(fileName), new StreamWriterFactory(), 64.KiloBytes()) { }
        public FixedFileWriterManger(string fileName, long maxSizeInBytes) : this(new FileNameProvider(fileName), new StreamWriterFactory(), maxSizeInBytes) { }

        public FixedFileWriterManger(IFileNameProvider fileNameProvider, IWriterFactory writerFactory, long maxSizeInBytes)
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
            
            currentWriter.Flush();
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