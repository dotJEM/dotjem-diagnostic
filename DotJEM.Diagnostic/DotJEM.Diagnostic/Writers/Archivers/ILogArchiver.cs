using DotJEM.Diagnostic.Writers.NonBlocking;

namespace DotJEM.Diagnostic.Writers.Archivers
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILogArchiver
    {
        void Archive(IWriterManger manager);
    }
}