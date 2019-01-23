using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Diagnostic.Collectors;
using DotJEM.Diagnostic.Correlation;
using DotJEM.Diagnostic.DataProviders;

namespace DotJEM.Diagnostic
{
    /// <summary>
    /// Provides a logger which uses the GetSystemTimePreciseAsFileTime api to provide high precision timestamps as well as a correlation ID's to track
    /// logical call contexts to provide a fast output format which can later be analyzed to provide timings of methods, blocks or entire requests etc.
    ///
    /// Each line in the log with consist of the high-precision timestamp, an identifier, the type followed by any custom data columns and finally the custom object.
    /// <code>
    /// 2019-01-23T13:27:05.7115313     eaff9f10.c07c6270.00000000  type    custom...data   { ...customObject.. }
    /// 2019-01-23T13:27:05.7104571     eaff9f10.be9bbefb.c07c6270  type    custom...data   { ...customObject.. }   
    /// 2019-01-23T13:27:05.6937642     eaff9f10.84f7412c.be9bbefb  type    custom...data   { ...customObject.. }  
    /// </code>
    ///
    /// <list type="table">
    ///   <listheader>  
    ///     <term>term</term>  
    ///     <description>description</description>  
    ///   </listheader>  
    ///   <item>  
    ///     <term>high-precision timestamp</term>  
    ///     <description>A timestamp produced by calling GetSystemTimePreciseAsFileTime which is only supported on newer systems. No fallback exists at this time.</description>  
    ///   </item>   
    ///   <item>  
    ///     <term>identifier</term>  
    ///     <description>
    ///        A identifier which includes RootScope.CurrentScope.ParentScope,
    ///        RootScope is the outer most scope started,
    ///        CurrentScope is the most recent started scope and
    ///        ParentScope is the scope just above that if any.
    ///     </description>  
    ///   </item>  
    /// </list>
    ///
    /// 
    /// </summary>
    public class HighPrecisionLogger : ILogger
    {
        private readonly ITraceEventCollector collector;
        private readonly Dictionary<string, ICustomDataProvider> providers;

        public HighPrecisionLogger(ITraceEventCollector collector) 
            : this(collector, ("Identity", new IdentityProvider()),
                ("Thread", new ThreadIdProvider()),
                ("Process", new ProcessIdProvider())
                )
        {
        }

        public HighPrecisionLogger(ITraceEventCollector collector, params (string Name, ICustomDataProvider Provider)[] providers)
            : this(collector, providers.ToDictionary(tuple => tuple.Name, tuple => tuple.Provider))
        {
        }

        public HighPrecisionLogger(ITraceEventCollector collector, Dictionary<string, ICustomDataProvider> providers)
        {
            this.providers = providers;
            this.collector = collector;
        }

        public async Task LogAsync(string type, object customData = null)
        {
            TraceEvent evt = new TraceEvent(type, HighResolutionTime.Now, CorrelationScope.Identifier, providers.Select(p => new CustomData(p.Key, p.Value.Data, p.Value.Format)), customData);
            await collector.Collect(evt);
        }
    }

    /// <summary>
    /// Provides an interface for a HighPrecisionLogger
    /// </summary>
    public interface ILogger
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

        Task LogAsync(string type, object customData = null);
    }

    public static class PerformanceLoggerExtensions
    {
        public static IPerformanceTracker Track(this ILogger self, string type, object customData = null)
        {
            self.LogAsync(">>> " + type, customData);
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
        private readonly ILogger logger;
        private volatile bool committed = false;

        public PerformanceTracker(ILogger logger, string type)
        {
            this.type = type;
            this.logger = logger;
        }

        public void Commit(object customData = null)
        {
            if (!committed)
            {
                logger.LogAsync("<<< " + type, customData);
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
