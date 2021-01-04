using System.Threading;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    public interface IThreadFactory
    {
        Thread Create(ThreadStart loop);
    }
}