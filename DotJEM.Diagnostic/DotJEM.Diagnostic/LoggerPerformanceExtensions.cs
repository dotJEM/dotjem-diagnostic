using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Diagnostic
{
    public static class LoggerPerformanceExtensions
    {
        public static IPerformanceTracker Track(this ILogger self, string type, JToken customData = null)
        {
            self.LogAsync(">>> " + type, customData);
            return new PerformanceTracker(self, type);
        }

        public static IPerformanceTracker Track(this ILogger self, string type, object customData)
            => self.Track(type, customData != null ? JToken.FromObject(customData) : null);

        public static void TrackAction(this ILogger self, Action action, object customData = null)
            => self.TrackAction(action, action.Method.Name, customData);

        public static void TrackAction(this ILogger self, Action action, string type, object customData = null)
        {
            using (self.Track(type, customData))
                action();
        }
    }
}