using System.Threading;

namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    public class NonBackgroundThreadThreadFactory : IThreadFactory
    {
        public Thread Create(ThreadStart loop) => new Thread(loop) { IsBackground = false };
    }
}