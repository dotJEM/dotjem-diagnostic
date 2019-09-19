using System;
using System.Transactions;
using DotJEM.Diagnostic.Common;
using DotJEM.Diagnostic.Correlation;
using Newtonsoft.Json.Linq;

namespace DotJEM.Diagnostic
{
    public static class LoggerPerformanceExtensions
    {
        public static IPerformanceTracker Track(this ILogger self, string type, JToken customData = null, bool correlationScope = true)
        {
            CorrelationScope scope = correlationScope 
                ? new CorrelationScope()
                : null;

            type = $"{type}:{IdProvider.Default.Next}";
            self.LogAsync("> " + type, customData);
            return new PerformanceTracker(self, type, scope);

        }

        public static IPerformanceTracker Track(this ILogger self, string type, object customData)
            => self.Track(type, TransformObject(customData));

        public static void TrackAction(this ILogger self, Action action, JToken customData = null)
            => self.TrackAction(action, action.Method.Name, customData);

        public static void TrackAction(this ILogger self, Action action, object customData)
            => self.TrackAction(action, action.Method.Name, customData);

        public static void TrackAction(this ILogger self, Action action, string type, object customData)
            => self.TrackAction(action, type, TransformObject(customData));

        public static void TrackAction(this ILogger self, Action action, string type, JToken customData = null)
        {
            using (self.Track(type, customData))
                action();
        }


        public static T TrackFunction<T>(this ILogger self, Func<T> func, JToken customData = null)
            => self.TrackFunction(func, func.Method.Name, customData);

        public static T TrackFunction<T>(this ILogger self, Func<T> func, object customData)
            => self.TrackFunction(func, func.Method.Name, customData);

        public static T TrackFunction<T>(this ILogger self, Func<T> func, string type, object customData)
            => self.TrackFunction(func, type, TransformObject(customData));

        public static T TrackFunction<T>(this ILogger self, Func<T> func, string type, JToken customData = null)
        {
            using (self.Track(type, customData))
                return func();
        }

        public static void Commit(this IPerformanceTracker self, object obj) 
            => self.Commit(TransformObject(obj));

        private static JToken TransformObject(object obj) => obj != null ? JToken.FromObject(obj) : null;
    }
}