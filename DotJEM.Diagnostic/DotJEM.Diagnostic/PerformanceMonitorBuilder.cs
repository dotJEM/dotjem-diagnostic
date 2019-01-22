using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Diagnostic.DataProviders;

namespace DotJEM.Diagnostic
{
    public interface IPerformanceMonitorBuilder
    {
        IPerformanceMonitorBuilder AddProvider(string name, ICustomDataProvider provider);
        IPerformanceMonitorBuilder ClearProviders();
        IPerformanceMonitorBuilder InsertProvider(int index, string name, ICustomDataProvider provider);
        IPerformanceMonitorBuilder RemoveProvider(string name);
        IPerformanceMonitorBuilder ReplaceOrAddProvider(string name, ICustomDataProvider provider);
        IPerformanceMonitor Build();
    }

    public class PerformanceMonitorBuilder : IPerformanceMonitorBuilder
    {
        private readonly HashSet<string> names;
        private readonly List<(string Name, ICustomDataProvider Provider)> providers;

        public PerformanceMonitorBuilder()
            : this(
                ("Identity", new IdentityProvider()),
                ("Thread", new ThreadIdProvider()),
                ("Process", new ProcessIdProvider())
            )
        {
        }

        public PerformanceMonitorBuilder(params (string Name, ICustomDataProvider Provider)[] providers)
        {
            this.providers = providers.ToList();
            this.names = new HashSet<string>(providers.Select(tuple => tuple.Name));
        }

        public IPerformanceMonitorBuilder ClearProviders()
        {
            names.Clear();
            providers.Clear();
            return this;
        }

        public IPerformanceMonitorBuilder InsertProvider(int index, string name, ICustomDataProvider provider)
        {
            if (names.Contains(name))
            {
                throw new ArgumentException("There is already a data provider with the given name.", nameof(name));
            }
            providers.Insert(index, (name, provider));
            names.Add(name);
            return this;
        }

        public IPerformanceMonitorBuilder AddProvider(string name, ICustomDataProvider provider)
        {
            if (names.Contains(name))
            {
                throw new ArgumentException("There is already a data provider with the given name.", nameof(name));
            }
            providers.Add((name, provider));
            names.Add(name);
            return this;
        }

        public IPerformanceMonitorBuilder RemoveProvider(string name)
        {
            if (names.Remove(name))
            {
                providers.RemoveAll(tuple => tuple.Name == name);
            }
            return this;
        }

        public IPerformanceMonitorBuilder ReplaceOrAddProvider(string name, ICustomDataProvider provider)
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

        public IPerformanceMonitor Build()
        {
            return new PerformanceMonitor(providers.ToDictionary(t => t.Name, t => t.Provider));
        }
    }
}