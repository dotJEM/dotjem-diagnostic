using System;
using DotJEM.Diagnostic.Common;
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

        public PerformanceTracker(ILogger logger, string type)
        {
            this.type = type;
            this.logger = logger;
        }
        
        public void Commit(JToken customData = null)
        {
            if (!Disposed)
            {
                logger.LogAsync("< " + type, customData);
            }
        }

        protected override void Dispose(bool disposing)
        {
            Commit();
            base.Dispose(disposing);
        }
    }
}