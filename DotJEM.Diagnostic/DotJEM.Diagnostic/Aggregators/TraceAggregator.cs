using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Diagnostic.Collectors;
using DotJEM.Diagnostic.Model;

namespace DotJEM.Diagnostic.Aggregators
{
    public class TraceEventAggregator : ITraceCollector<TraceEvent>
    {
        private long gen = 0;
        private readonly ConcurrentDictionary<int, TraceEventTreeBuilder> inflow = new();
        private readonly ITraceCollector<TraceEventGraph> collector;

        public TraceEventAggregator(ITraceCollector<TraceEventGraph> collector)
        {
            this.collector = collector;
        }
        public Task Collect(TraceEvent trace)
        {
            long nextGen = Interlocked.Increment(ref gen);
            int id = BitConverter.ToInt32(trace.Correlation.CorrelatorId, 0);
            TraceEventTreeBuilder treeBuilder = inflow
                .AddOrUpdate(id, i => new TraceEventTreeBuilder(), (i, c) => c.Capture(nextGen, trace));

            if (!treeBuilder.IsClosed(nextGen))
                return Task.CompletedTask;

            if (inflow.TryRemove(id, out treeBuilder))
                Publish(treeBuilder);
            return Task.CompletedTask;
        }

        private void Publish(TraceEventTreeBuilder builder)
        {
            collector.Collect(builder.Build());
        }

    }

    public class TraceEventTreeBuilder
    {
        public static long Window = 16 * 1024;

        private long gen;
        private readonly TraceEventNodeBuilder root = new();
        private readonly List<TraceEvent> events = new();
        private readonly Dictionary<int, TraceEventNodeBuilder> map = new();


        public TraceEventTreeBuilder Capture(long nextGen, TraceEvent trace)
        {
            gen = nextGen;
            events.Add(trace);

            int id = BitConverter.ToInt32(trace.Correlation.EventId, 0);
            if (map.TryGetValue(id, out TraceEventNodeBuilder node))
            {
                node.AddEvent(trace);
                return this;
            }

            int parent = BitConverter.ToInt32(trace.Correlation.ParentId, 0);
            if (map.TryGetValue(parent, out node))
            {
                map.Add(id, node.AddNode(new TraceEventNodeBuilder().AddEvent(trace)));
                return this;
            }

            map.Add(id, root.AddEvent(trace));
            return this;
        }

        public TraceEventGraph Build()
        {
            return new TraceEventGraph(events, root.Build());
        }

        public bool IsClosed(long nextGen) => root.IsClosed || nextGen - Window > gen;
    }

    public class TraceEventNodeBuilder
    {
        private TraceEvent @in;
        private TraceEvent @out;
        private readonly List<TraceEvent> messages = new();
        private readonly List<TraceEventNodeBuilder> nodes = new();

        public bool IsClosed => nodes.All(n => n.IsClosed) && @out != null;

        public TraceEventNodeBuilder AddEvent(TraceEvent trace)
        {
            switch (trace.Type[0])
            {
                case '>':
                    @in = trace;
                    return this;

                case '<':
                    @out = trace;
                    return this;

                default:
                    messages.Add(trace);
                    return this;
            }
        }

        public TraceEventNodeBuilder AddNode(TraceEventNodeBuilder node)
        {
            nodes.Add(node);
            return node;
        }

        public TraceEventNode Build()
        {
            var buildNodes = nodes.Select(n => n.Build()).ToArray();
            if (@in == null)
            {
                DateTime time = messages
                    .Select(m => m.Time)
                    .Concat(buildNodes.Select(n => n.Start))
                    .Concat(@out != null ? new[] { @out.Time } : new DateTime[0])
                    .Min();
                //NULL HERE
                if (@out == null)

                {
                    TraceEvent first = messages.First();
                    @in = new TraceEvent(">" + first.Type.Remove(0, 1), time, first.Correlation, first.CustomData, null);
                }
                else
                {
                    @in = new TraceEvent(">" + @out.Type.Remove(0, 1), time, @out.Correlation, @out.CustomData, null);
                }
            }
            return new(@in, @out, messages, buildNodes);
        }
    }

}
