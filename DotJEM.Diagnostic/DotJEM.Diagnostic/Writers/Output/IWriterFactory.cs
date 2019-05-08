using System.IO;
using System.Threading;

namespace DotJEM.Diagnostic.Writers.Output
{
    public interface IWriterFactory
    {
        bool TryOpen(string path, out ITextWriter writer);
        bool TryOpenWithRetries(string path, int maxTries, CancellationToken cancellation, out ITextWriter writer);
    }

    public class StreamWriterFactory : IWriterFactory
    {
        public bool TryOpen(string path, out ITextWriter writer)
        {
            try
            {
                FileInfo file = new FileInfo(path);
                StreamWriter streamWriter = new StreamWriter(path, true);
                writer = new TextWriterProxy(streamWriter, file.Length);
                return true;
            }
            catch
            {
                writer = null;
                return false;
            }
        }


        public bool TryOpenWithRetries(string path, int maxTries, CancellationToken cancellation, out ITextWriter writer)
        {
            writer = null;
            for (int i = 0; i < maxTries; i++)
            {
                if (cancellation.IsCancellationRequested)
                    return false;

                if (TryOpen(path, out writer))
                    return true;

                if (i > 3)
                    Thread.Sleep(i * 10);
            }
            return false;
        }
    }
}