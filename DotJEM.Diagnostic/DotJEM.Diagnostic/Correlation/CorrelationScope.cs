using System.Threading;
using DotJEM.Diagnostic.Common;

namespace DotJEM.Diagnostic.Correlation
{
    public class CorrelationScope : Disposable
    {
        private static readonly AsyncLocal<CorrelationScope> current = new AsyncLocal<CorrelationScope>();

        public static CorrelationScope Current => current.Value;
        public static string Identifier => Current?.ToString() ?? "00000000.00000000.00000000";

        public string Id { get; }
        public string CorrelationId { get; }
        public CorrelationScope Parent { get; }

        public CorrelationScope()
        {
            Parent = Current;

            Id = IdProvider.Default.Next;
            //CorrelationId = Parent?.CorrelationId ?? IdProvider.Default.Next;
            //Note: This saves us some time and shouldn't really cause any correlation issues, if it does we can reinstate a unique ID for the Root.
            CorrelationId = Parent?.CorrelationId ?? Id;

            current.Value = this;
        }

        protected override void Dispose(bool disposing)
        {
            current.Value = Parent;
            base.Dispose(disposing);
        }

        public override string ToString() => $"{CorrelationId}.{Id}.{Parent?.Id??"00000000"}";
    }
}