using System.Threading;

namespace DotJEM.Diagnostic.Correlation
{
    public class CorrelationScope : Disposable
    {
        public static AsyncLocal<CorrelationScope> Current { get; } = new AsyncLocal<CorrelationScope>();
        public string Id { get; }
        public string CorrelationId { get; }
        public CorrelationScope Parent { get; }

        public CorrelationScope()
        {
            Parent = Current.Value;

            Id = IdProvider.Default.Next;
            CorrelationId = Parent?.CorrelationId ?? IdProvider.Default.Next;

            Current.Value = this;
        }

        protected override void Dispose(bool disposing)
        {
            Current.Value = Parent;
            base.Dispose(disposing);
        }

        public override string ToString()
        {
            return $"{CorrelationId}.{Id}.{Parent?.Id??"00000000"}";
        }
    }
}