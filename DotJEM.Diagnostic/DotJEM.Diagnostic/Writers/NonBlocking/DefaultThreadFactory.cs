using System.Threading;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    public class DefaultThreadFactory : IThreadFactory
    {
        public Thread Create(ThreadStart loop) => new Thread(loop) { IsBackground = true };
    }
}