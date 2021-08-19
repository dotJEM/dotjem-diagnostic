using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DotJEM.Diagnostic.Writers.NonBlocking;

namespace DotJEM.Diagnostic.Writers.Archivers
{
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
}