using System.IO;

namespace DotJEM.Diagnostic.Writers
{
    public interface ITextWriter
    {
        long Size { get; }
        void Close();
        void WriteLine(string value);
        void WriteLines(params string[] values);
        void Flush();
    }

    public class TextWriterProxy : ITextWriter
    {
        private readonly TextWriter writer;
        private readonly int newLineByteCount;

        public long Size { get; private set; }

        public TextWriterProxy(TextWriter current, long currentSize)
        {
            Size = currentSize;
            writer = current;
            newLineByteCount = writer.Encoding.GetByteCount(writer.NewLine);
        }

        public void WriteLine(string value)
        {
            //Note: This is significantly faster than having to refresh the file each time.
            //      As an added bonus, we don't have to flush explicitly each time to get the
            //      size which only saves even more time. That doesn't exclude us from
            //      explicitly flushing in other scenarios to ensure that data is written to the disk though.

            Size += writer.Encoding.GetByteCount(value) + newLineByteCount;
            writer.WriteLine(value);
        }

        public void Write(string value)
        {
            Size += writer.Encoding.GetByteCount(value);
            writer.Write(value);
        }

        public void WriteLines(params string[] values)
        {
            if (values.Length < 1)
                return;
            WriteLine(string.Join(writer.NewLine, values));
        }

        public void Flush() => writer.Flush();
        public void Close() => writer.Close();
    }

}