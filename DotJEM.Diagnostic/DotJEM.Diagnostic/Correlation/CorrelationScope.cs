using System.Threading;

namespace DotJEM.Diagnostic.Correlation
{
    public class CorrelationScope : Disposable
    {
        private static readonly AsyncLocal<CorrelationScope> current = new AsyncLocal<CorrelationScope>();

        public static CorrelationScope Current => current.Value;

        public string Id { get; }
        public string CorrelationId { get; }
        public CorrelationScope Parent { get; }

        public CorrelationScope()
        {
            Parent = Current;

            Id = IdProvider.Default.Next;
            CorrelationId = Parent?.CorrelationId ?? IdProvider.Default.Next;

            current.Value = this;
        }

        protected override void Dispose(bool disposing)
        {
            current.Value = Parent;
            base.Dispose(disposing);
        }

        public override string ToString()
        {
            return $"{CorrelationId}.{Id}.{Parent?.Id??"00000000"}";
        }
    }
}