﻿using System;
using System.Collections.Generic;
using System.IO;
using DotJEM.Diagnostic.Writers.NonBlocking;

namespace DotJEM.Diagnostic.Writers
{
    /// <summary>
    /// Provides file names to a <see cref="IWriterManger"/>.
    /// </summary>
    public interface IFileNameProvider : IFileLister
    {
        string Directory { get; }
        string FullName { get; }
        string Id(int id);
        string Unique();
        string Unique(string ext);
    }

    public class FileNameProvider : IFileNameProvider
    {
        public string Directory { get; }
        public string Name { get; }
        public string Extension { get; }
        public string FullName { get; }

        public FileNameProvider(string fileName)
        {
            this.FullName = fileName;
            this.Directory = Path.GetDirectoryName(fileName);
            this.Name = Path.GetFileNameWithoutExtension(fileName);
            this.Extension = Path.GetExtension(fileName)?.TrimStart('.') ?? "log";
        }

        public string Id(int id) => Path.Combine(Directory, $"{Name}-{id:x8}.{Extension}");
        public string Unique() => Unique(Extension);
        public string Unique(string ext) => Path.Combine(Directory, $"{Name}-{Guid.NewGuid():N}.{ext.TrimStart('.')}");
        public IEnumerable<FileInfo> AllFiles(string extension = null) => new DirectoryInfo(Directory).EnumerateFiles($"{Name}-*.{extension?.TrimStart('.') ?? Extension}");
    }
}