using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Diagnostic.Collectors;
using DotJEM.Diagnostic.DataProviders;

namespace DotJEM.Diagnostic
{
    public interface ILoggerBuilder
    {
        ILoggerBuilder AddCollector(ITraceEventCollector collector);
        ILoggerBuilder AddCollector(Func<ITraceEventCollectorBuilder, ITraceEventCollectorBuilder> factory);
        ILoggerBuilder ClearCollectors();

        ILoggerBuilder AddProvider(string name, ICustomDataProvider provider);
        ILoggerBuilder ClearProviders();
        ILoggerBuilder InsertProvider(int index, string name, ICustomDataProvider provider);
        ILoggerBuilder RemoveProvider(string name);
        ILoggerBuilder ReplaceOrAddProvider(string name, ICustomDataProvider provider);
        ILogger Build();
    }

    public interface ITraceEventCollectorBuilder
    {
        ITraceEventCollectorBuilder Add(ITraceEventCollector collector);
        ITraceEventCollectorBuilder Clear();

        ITraceEventCollector Build();
    }

    public class CompositeTraceEventCollectorBuilder : ITraceEventCollectorBuilder
    {
        private readonly List<ITraceEventCollector> collectors = new List<ITraceEventCollector>();

        public ITraceEventCollectorBuilder Add(ITraceEventCollector collector)
        {
            collectors.Add(collector);
            return this;
        }

        public ITraceEventCollectorBuilder Clear()
        {
            collectors.Clear();
            return this;
        }

        public ITraceEventCollector Build()
        {
            if(!collectors.Any())
                return new NullTraceEventCollector();

            if (collectors.Count == 1)
                return collectors.Single();

            return new CompositeTraceEventCollector(collectors);
        }
    }

    public class HighPrecisionLoggerBuilder : ILoggerBuilder
    {
        private readonly HashSet<string> names;
        private readonly List<(string Name, ICustomDataProvider Provider)> providers;
        private readonly ITraceEventCollectorBuilder collector;

        public HighPrecisionLoggerBuilder()
        {
            this.collector = new CompositeTraceEventCollectorBuilder();
            this.providers = new List<(string Name, ICustomDataProvider Provider)>
            {
                ("Identity", new IdentityProvider()),
                ("Thread", new ThreadIdProvider()),
                ("Process", new ProcessIdProvider())
            };
            this.names = new HashSet<string>(providers.Select(tuple => tuple.Name));
        }
        
        
        public ILoggerBuilder ClearCollectors()
        {
            this.collector.Clear();
            return this;
        }


        public ILoggerBuilder AddCollector(ITraceEventCollector collector)
        {
            this.collector.Add(collector);
            return this;
        }


        public ILoggerBuilder AddCollector(Func<ITraceEventCollectorBuilder, ITraceEventCollectorBuilder> factory) 
            => AddCollector(factory(new CompositeTraceEventCollectorBuilder()).Build());


        public ILoggerBuilder ClearProviders()
        {
            names.Clear();
            providers.Clear();
            return this;
        }

        public ILoggerBuilder InsertProvider(int index, string name, ICustomDataProvider provider)
        {
            if (names.Contains(name))
            {
                throw new ArgumentException("There is already a data provider with the given name.", nameof(name));
            }
            providers.Insert(index, (name, provider));
            names.Add(name);
            return this;
        }

        public ILoggerBuilder AddProvider(string name, ICustomDataProvider provider)
        {
            if (names.Contains(name))
            {
                throw new ArgumentException("There is already a data provider with the given name.", nameof(name));
            }
            providers.Add((name, provider));
            names.Add(name);
            return this;
        }

        public ILoggerBuilder RemoveProvider(string name)
        {
            if (names.Remove(name))
            {
                providers.RemoveAll(tuple => tuple.Name == name);
            }
            return this;
        }

        public ILoggerBuilder ReplaceOrAddProvider(string name, ICustomDataProvider provider)
        {
            if (names.Contains(name))
            {
                int index = providers.FindIndex(tuple => tuple.Name == name);
                providers[index] = (name, provider);
            }
            else
            {
                AddProvider(name, provider);
            }
            return this;
        }

        public ILogger Build()
        {
            return new HighPrecisionLogger(collector.Build(), providers.ToDictionary(t => t.Name, t => t.Provider));
        }
    }
}