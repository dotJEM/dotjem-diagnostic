using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotJEM.Diagnostic.Writers.NonBlocking;

namespace DotJEM.Diagnostic.Writers.Archivers
{
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
}