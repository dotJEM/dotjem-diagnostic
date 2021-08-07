using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Diagnostic.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class TraceEventGraph
    {
        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<TraceEvent> Lines { get; }

        /// <summary>
        /// 
        /// </summary>
        public TraceEventNode Root { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="root"></param>
        public TraceEventGraph(IReadOnlyList<TraceEvent> lines, TraceEventNode root)
        {
            Lines = lines;
            Root = root;
        }

        public override string ToString() => ToString(true);
        public string ToString(bool condensed) => JObject.FromObject(this).ToString(condensed ? Formatting.None : Formatting.Indented);
    }

    /// <summary>
    /// 
    /// </summary>
    public class TraceEventNode
    {
        public DateTime Start => In.Time; // Note -> IF we don't have IN, we try to use OUT to have a marker.
        public DateTime? Stop => Out?.Time ?? In.Time; // Note -> IF we don't have OUT, we try to use IN to have a marker.
        public TimeSpan Duration => Stop?.Subtract(Start) ?? TimeSpan.Zero;
        public TraceEvent In { get; }
        public TraceEvent Out { get; }
        public IReadOnlyList<TraceEvent> Messages { get; }
        public IReadOnlyList<TraceEventNode> Nodes { get; }

        public TraceEventNode(TraceEvent @in, TraceEvent @out, IEnumerable<TraceEvent> messages, IEnumerable<TraceEventNode> nodes)
        {
            Messages = messages.ToArray();
            Nodes = nodes.ToArray();

            In = @in;
            Out = @out;
        }

    }
}