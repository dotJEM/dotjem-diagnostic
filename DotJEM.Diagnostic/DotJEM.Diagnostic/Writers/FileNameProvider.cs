using System;
using System.IO;

namespace DotJEM.Diagnostic.Writers
{
    public interface IFileNameProvider
    {
        string FullName { get; }
        string Id(int id);
        string Unique();
        string Unique(string ext);
    }

    public class FileNameProvider : IFileNameProvider
    {
        private readonly string directory;
        private readonly string name;
        private readonly string ext;

        public string FullName { get; }

        public FileNameProvider(string fileName)
        {
            this.FullName = fileName;
            this.directory = Path.GetDirectoryName(fileName);
            this.name = Path.GetFileNameWithoutExtension(fileName);
            this.ext = Path.GetExtension(fileName);
        }

        public string Id(int id) => Path.Combine(directory, $"{name}-{id:x}{ext}");
        public string Unique() => Unique(ext);
        public string Unique(string ext) => Path.Combine(directory, $"{name}-{Guid.NewGuid():N}{ext}");
    }
}