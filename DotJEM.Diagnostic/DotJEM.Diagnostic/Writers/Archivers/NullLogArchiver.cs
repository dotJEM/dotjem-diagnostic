using DotJEM.Diagnostic.Writers.NonBlocking;

namespace DotJEM.Diagnostic.Writers.Archivers
{
    public class NullLogArchiver : ILogArchiver
    {
        public void Archive(IWriterManger manager)
        {
            //NO-OP
        }
    }
}