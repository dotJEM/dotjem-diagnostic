using System;
using System.Transactions;
using DotJEM.Diagnostic.Common;
using DotJEM.Diagnostic.Correlation;
using Newtonsoft.Json.Linq;

namespace DotJEM.Diagnostic
{
    public static class LoggerPerformanceExtensions
    {

        public static IPerformanceTracker Track(this ILogger self, string type, JToken customData = null, CorrelationScope correlationScope = null)
        {
            type = correlationScope != null
                ? type
                : $"{type}:{IdProvider.Default.Next}";

            self.LogAsync("> " + type, customData);
            return new PerformanceTracker(self, type, correlationScope);
        }

        public static IPerformanceTracker Track(this ILogger self, string type, JToken customData = null, bool createCorrelationScope = true)
            => self.Track(type, customData, createCorrelationScope ? new CorrelationScope() : null);

        public static IPerformanceTracker Track(this ILogger self, string type, object customData, bool createCorrelationScope = true)
            => self.Track(type, TransformObject(customData), createCorrelationScope ? new CorrelationScope() : null);

        public static IPerformanceTracker Track(this ILogger self, string type, object customData, CorrelationScope correlationScope = null)
            => self.Track(type, TransformObject(customData), correlationScope);

        public static void TrackAction(this ILogger self, Action action, JToken customData = null, bool createCorrelationScope = true)
            => self.TrackAction(action, action.Method.Name, customData, createCorrelationScope);

        public static void TrackAction(this ILogger self, Action action, object customData, bool createCorrelationScope = true)
            => self.TrackAction(action, action.Method.Name, customData, createCorrelationScope);

        public static void TrackAction(this ILogger self, Action action, string type, object customData, bool createCorrelationScope = true)
            => self.TrackAction(action, type, TransformObject(customData), createCorrelationScope);

        public static void TrackAction(this ILogger self, Action action, string type, JToken customData = null, bool createCorrelationScope = true)
            => self.TrackAction(action, type, customData, createCorrelationScope ? new CorrelationScope() : null);



        public static void TrackAction(this ILogger self, Action action, JToken customData = null, CorrelationScope correlationScope = null)
            => self.TrackAction(action, action.Method.Name, customData, correlationScope);

        public static void TrackAction(this ILogger self, Action action, object customData, CorrelationScope correlationScope = null)
            => self.TrackAction(action, action.Method.Name, customData, correlationScope);

        public static void TrackAction(this ILogger self, Action action, string type, object customData, CorrelationScope correlationScope = null)
            => self.TrackAction(action, type, TransformObject(customData), correlationScope);
        
        public static void TrackAction(this ILogger self, Action action, string type, JToken customData = null, CorrelationScope correlationScope = null)
        {
            using (self.Track(type, customData, correlationScope))
                action();
        }



        public static T TrackFunction<T>(this ILogger self, Func<T> func, JToken customData = null, bool createCorrelationScope = true)
            => self.TrackFunction(func, func.Method.Name, customData, createCorrelationScope);

        public static T TrackFunction<T>(this ILogger self, Func<T> func, object customData, bool createCorrelationScope = true)
            => self.TrackFunction(func, func.Method.Name, customData, createCorrelationScope);

        public static T TrackFunction<T>(this ILogger self, Func<T> func, string type, object customData, bool createCorrelationScope = true)
            => self.TrackFunction(func, type, TransformObject(customData), createCorrelationScope);

        public static T TrackFunction<T>(this ILogger self, Func<T> func, string type, JToken customData = null, bool createCorrelationScope = true)
            => self.TrackFunction(func, type, customData, createCorrelationScope ? new CorrelationScope() : null);


        public static T TrackFunction<T>(this ILogger self, Func<T> func, JToken customData = null, CorrelationScope correlationScope = null)
            => self.TrackFunction(func, func.Method.Name, customData, correlationScope);

        public static T TrackFunction<T>(this ILogger self, Func<T> func, object customData, CorrelationScope correlationScope = null)
            => self.TrackFunction(func, func.Method.Name, customData, correlationScope);

        public static T TrackFunction<T>(this ILogger self, Func<T> func, string type, object customData, CorrelationScope correlationScope = null)
            => self.TrackFunction(func, type, TransformObject(customData), correlationScope);


        public static T TrackFunction<T>(this ILogger self, Func<T> func, string type, JToken customData = null, CorrelationScope correlationScope = null)
        {
            using (self.Track(type, customData, correlationScope))
                return func();
        }

        public static void Commit(this IPerformanceTracker self, object obj) 
            => self.Commit(TransformObject(obj));

        private static JToken TransformObject(object obj) => obj != null ? JToken.FromObject(obj) : null;
    }
}