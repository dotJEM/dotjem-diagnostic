namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    public static class WriterManagerExtensions
    {
        public static bool Close(this IWriterManger self)
        {
            self.Acquire(out bool replaced).Close();
            return replaced;
        }

        public static bool WriteLine(this IWriterManger self, string value)
        {
            self.Acquire(out bool replaced).WriteLine(value);
            return replaced;
        }

        public static bool WriteLines(this IWriterManger self, params string[] values)
        {
            self.Acquire(out bool replaced).WriteLines(values);
            return replaced;
        }

        public static bool Flush(this IWriterManger self)
        {
            self.Acquire(out bool replaced).Flush();
            return replaced;
        }
    }
}