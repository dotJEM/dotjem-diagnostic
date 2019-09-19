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

    public class PerformanceTracker : Disposable, IPerformanceTracker
    {
        private readonly string type;
        private readonly ILogger logger;
        private readonly IDisposable scope;

        public PerformanceTracker(ILogger logger, string type, IDisposable scope)
        {
            this.type = type;
            this.scope = scope;
            this.logger = logger;
        }
        
        public void Commit(JToken customData = null)
        {
            if (Disposed)
                return;

            logger.LogAsync("< " + type, customData);
        }

        protected override void Dispose(bool disposing)
        {
            Commit();
            scope?.Dispose();
            base.Dispose(disposing);
        }
    }
}