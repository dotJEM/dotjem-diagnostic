namespace DotJEM.Diagnostic.Writers.Formatter
{
    public interface ITraceFormatter<in TEvent>
    {
        string Format(TEvent evt);
    }

    public class DefaultTraceFormatter<TEvent> : ITraceFormatter<TEvent>
    {
        public string Format(TEvent evt) => evt.ToString();
    }
}