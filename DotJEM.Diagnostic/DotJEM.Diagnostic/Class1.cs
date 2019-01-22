using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using DotJEM.Diagnostic.Correlation;
using DotJEM.Diagnostic.DataProviders;

namespace DotJEM.Diagnostic
{


    //Fixed CustomData Provider!


    public class TraceEvent
    {
        public string Type { get; }
        public DateTimeOffset Time { get; } = HighResolutionDateTime.Now;
        public string Correlation { get; } = CorrelationScope.Current?.ToString() ?? "00000000.00000000.00000000";
        public CustomData[] CustomData { get; }
        public object CustomObject { get; }

        public TraceEvent(string type, IEnumerable<CustomData> customData, object customObject)
        {
            Type = type;
            CustomData = customData.ToArray();
            CustomObject = customObject;
        }
        /*
         * Log as:
         *
         * Time                            Id                         Thread     CustomFields        CustomData
         * 2018-11-30T11:16:52.657862      02efb36.b992e58.0000000    47         JMD     >>>         { }
         * 2018-11-30T11:16:53.657862      02efb36.fe92a58.b992e58    134        JMD     >>>         { }
         * 2018-11-30T11:16:54.657862      02efb36.e452fe8.fe92a58    200        JMD     >>>         { }
         * 2018-11-30T11:16:54.757862      02efb36.aef928a.e452fe8    42         JMD     <<<         { }
         * 2018-11-30T11:16:56.657862      02efb36.a129ee8.fe92a58    9          JMD     >>>         { }
         *
         * Custom fields are custom provider for fixed length fields, this can be identity of the user, start/stop signalling etc
         * 
         * CustomData is a custom data object, which is ToString'ed... Each aditional line will be prepended with a indentation to
         * identify lines belonging to the statement. CustomData is merely a Custom field provider
         *
         *
         */
        public override string ToString() => $"{Time:O}\t{Correlation}\t{Type}\t{string.Join("\t", CustomData.Select(data => data.ToString()))}\t{CustomObject}";
    }

    public class CustomData
    {
        private readonly string format;

        public string Name { get; }
        public object Value { get; }

        public CustomData(string name, object value, string format)
        {
            //TODO: Formatter instead!
            this.format = string.IsNullOrWhiteSpace(format) ? "{0}" : $"{{0:{format}}}";
            Name = name;
            Value = value;
        }

        public override string ToString() => string.Format(format, Value);
    }

    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly Dictionary<string, ICustomDataProvider> providers;

        public PerformanceMonitor() 
            : this(
                ("Identity", new IdentityProvider()),
                ("Thread", new ThreadIdProvider()),
                ("Process", new ProcessIdProvider())
                )
        {
        }

        public PerformanceMonitor(params (string Name, ICustomDataProvider Provider)[] providers)
            : this(providers.ToDictionary(tuple => tuple.Name, tuple => tuple.Provider))
        {
        }

        public PerformanceMonitor(Dictionary<string, ICustomDataProvider> providers)
        {
            this.providers = providers;
        }

        public void Trace(string type, object customData = null)
        {
            TraceEvent evt = new TraceEvent(type, providers.Select(p => new CustomData(p.Key, p.Value.Data, p.Value.Format)), customData);
            
            Console.WriteLine(evt);

        }
    }



    public interface IPerformanceMonitor
    {
        //bool Enabled { get; }
        //IDiagnosticsLogger Diag { get; }

        //IPerformanceTracker Track(string type, params object[] args);
        //IPerformanceTracker TrackRequest(HttpRequestMessage request);
        //IPerformanceTracker TrackTask(string name);

        //void TrackAction(Action action, params object[] args);
        //void TrackAction(string name, Action action, params object[] args);

        //T TrackFunction<T>(Func<T> func, params object[] args);
        //T TrackFunction<T>(string name, Func<T> func, params object[] args);

        //void LogSingleEvent(string type, long elapsed, params object[] args);

        //IPerformanceTracker Track(string type);
        //IPerformanceTracker Track(string type, params object[] args);
        //IPerformanceTracker Track(string type, string json);
        //IPerformanceTracker Track(string type, JObject json);

        void Trace(string type, object customData = null);
    }

    public static class PerformanceMonitorExtensions
    {
        public static IPerformanceTracker Track(this IPerformanceMonitor self, string type, object customData = null)
        {
            self.Trace(">>> " + type, customData);
            return new PerformanceTracker(self, type);
        }
    }

    public interface IPerformanceTracker : IDisposable
    {
        void Commit(object customData = null);
    }

    public class PerformanceTracker : Disposable, IPerformanceTracker
    {
        private readonly string type;
        private readonly IPerformanceMonitor logger;
        private volatile bool committed = false;

        public PerformanceTracker(IPerformanceMonitor logger, string type)
        {
            this.type = type;
            this.logger = logger;
        }

        public void Commit(object customData = null)
        {
            if (committed)
            {
                logger.Trace("<<< " + type, customData);
            }
            committed = true;
        }

        protected override void Dispose(bool disposing)
        {
            Commit();
            base.Dispose(disposing);
        }
    }
}
