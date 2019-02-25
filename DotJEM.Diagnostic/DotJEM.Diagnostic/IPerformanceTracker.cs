using System;
using DotJEM.Diagnostic.Common;

namespace DotJEM.Diagnostic
{
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