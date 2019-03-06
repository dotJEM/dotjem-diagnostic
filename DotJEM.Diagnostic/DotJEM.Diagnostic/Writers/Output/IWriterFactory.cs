using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.Diagnostic.Writers
{
    public interface IWriterFactory
    {
        bool TryOpen(string path, out ITextWriter writer);
        Task<ITextWriter> TryOpenWithRetries(string path);
        Task<ITextWriter> TryOpenWithRetries(string path, int maxTries);
        Task<ITextWriter> TryOpenWithRetries(string path, CancellationToken cancellation);
        Task<ITextWriter> TryOpenWithRetries(string path, int maxTries, CancellationToken cancellation);
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

        public Task<ITextWriter> TryOpenWithRetries(string path)
            => TryOpenWithRetries(path, 100, CancellationToken.None);

        public Task<ITextWriter> TryOpenWithRetries(string path, int maxTries)
            => TryOpenWithRetries(path, maxTries, CancellationToken.None);

        public Task<ITextWriter> TryOpenWithRetries(string path, CancellationToken cancellation)
            => TryOpenWithRetries(path, 100, cancellation);

        public async Task<ITextWriter> TryOpenWithRetries(string path, int maxTries, CancellationToken cancellation)
        {
            for (int i = 0; i < maxTries; i++)
            {
                if (cancellation.IsCancellationRequested)
                    return null;

                if (TryOpen(path, out ITextWriter writer))
                    return writer;

                if (i > maxTries / 10)
                    await Task.Delay(i * 10, cancellation).ConfigureAwait(false);
            }
            return null;
        }
    }
}