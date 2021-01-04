using DotJEM.Diagnostic.Writers.Output;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    public interface IWriterManger : IFileLister
    {
        IFileNameProvider NameProvider { get; }
        ITextWriter Acquire();
        ITextWriter Acquire(out bool replaced);
    }
}