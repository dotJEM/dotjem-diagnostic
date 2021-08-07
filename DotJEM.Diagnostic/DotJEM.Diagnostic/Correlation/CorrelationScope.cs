using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using DotJEM.Diagnostic.Common;

namespace DotJEM.Diagnostic.Correlation
{
    public sealed class CorrelationScope : IDisposable
    {
        private static readonly AsyncLocal<CorrelationScope> current = new AsyncLocal<CorrelationScope>();

        public static CorrelationScope Current => current.Value;
        public static CorrelatorToken CurrentToken => Current?.Token ?? CorrelatorToken.EMPTY;

        public CorrelatorToken Token { get; }
        public bool Isolated { get; }
        public CorrelationScope Parent { get; }

        public CorrelationScope(bool isolated = false)
            : this(IdProvider.Default.NextBytes, isolated)
        {
        }

        public CorrelationScope(Guid source, bool isolated = false)
            : this(IdProvider.Default.ComputeBytes(source), isolated)
        {
        }

        private CorrelationScope(byte[] id, bool isolated)
        {
            Parent = Current;
            Isolated = isolated;
            //Note: This saves us some time and shouldn't really cause any correlation issues, if it does we can reinstate a unique ID for the Root.
            Token = new CorrelatorToken(
                isolated || Parent == null ? id : Parent.Token.CorrelatorId,
                id,
                (Parent != null ? Parent.Token.EventId : CorrelatorToken.EMPTY_BYTES)
            );
            current.Value = this;
        }

        public void Dispose() => current.Value = Parent;

        public override string ToString() => Token.ToString();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public readonly struct CorrelatorToken
    {
        public static readonly byte[] EMPTY_BYTES = {0, 0, 0, 0};
        public static readonly CorrelatorToken EMPTY = new CorrelatorToken(EMPTY_BYTES, EMPTY_BYTES, EMPTY_BYTES);

        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] CorrelatorId { get; }

        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] EventId { get; }

        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] ParentId { get; }

        public string CorrelatorStrId => BytesToString(CorrelatorId);
        public string EventStrId => BytesToString(EventId);
        public string ParentStrId => BytesToString(ParentId);

        public CorrelatorToken(byte[] correlatorId, byte[] eventId, byte[] parentId)
        {
            CorrelatorId = correlatorId;
            EventId = eventId;
            ParentId = parentId;
        }

        public override string ToString()
        {
            return $"{CorrelatorStrId}.{EventStrId}.{ParentStrId}";
        }

        private static string BytesToString(byte[] bytes) => string.Concat(bytes.Select(b => b.ToString("x2")));
    }
}