using System.Collections.Generic;
using System.IO;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    public interface IFileLister
    {
        IEnumerable<FileInfo> AllFiles(string extension = null);
    }
}