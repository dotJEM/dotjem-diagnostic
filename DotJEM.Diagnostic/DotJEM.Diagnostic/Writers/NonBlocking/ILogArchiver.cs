namespace DotJEM.Diagnostic.Writers.NonBlocking
{
    public interface ILogArchiver
    {
        void Archive(IWriterManger manager);
    }
}