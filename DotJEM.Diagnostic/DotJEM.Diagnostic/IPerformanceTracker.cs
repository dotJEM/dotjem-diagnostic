using System;
using System.Diagnostics;
using System.IO;
using DotJEM.Diagnostic.Common;
using DotJEM.Diagnostic.Correlation;
using Newtonsoft.Json.Linq;

namespace DotJEM.Diagnostic
{
    public interface IPerformanceTracker : IDisposable
    {
        void Commit(JToken customData = null);
    }

    public class NullPerformanceTracker : IPerformanceTracker
    {
        public static NullPerformanceTracker SharedInstance = new NullPerformanceTracker();

        public void Commit(JToken customData = null) { }

        public void Dispose() { }
    }

    public class PerformanceTracker : Disposable, IPerformanceTracker
    {
        private readonly string type;
        private readonly ILogger logger;
        private IDisposable scope;
        private object padlock = new object();
        private bool complete;

        public PerformanceTracker(ILogger logger, string type, IDisposable scope)
        {
            this.type = type;
            this.scope = scope;
            this.logger = logger;
        }
        
        public void Commit(JToken customData = null)
        {
            if (Disposed || complete)
                throw new ObjectDisposedException("Cannot commit extra data to a disposed tracker.");

            logger.LogAsync("- " + type, customData);
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed || complete)
                return;

            lock (padlock)
            {
                if (complete)
                    return;
                complete = true;
            }

            logger.LogAsync("< " + type);
            scope?.Dispose();
            scope = null;

            base.Dispose(disposing);
        }
    }
}