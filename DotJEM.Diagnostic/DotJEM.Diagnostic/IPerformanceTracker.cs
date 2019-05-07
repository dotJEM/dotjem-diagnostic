﻿using System;
using DotJEM.Diagnostic.Common;
using Newtonsoft.Json.Linq;

namespace DotJEM.Diagnostic
{
    public interface IPerformanceTracker : IDisposable
    {
        void Commit(object customData);
        void Commit(JToken customData = null);
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

        public void Commit(object customData)
        {
            throw new NotImplementedException();
        }

        public void Commit(JToken customData = null)
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