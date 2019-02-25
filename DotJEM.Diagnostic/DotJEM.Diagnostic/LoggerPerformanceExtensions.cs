namespace DotJEM.Diagnostic
{
    public static class LoggerPerformanceExtensions
    {
        public static IPerformanceTracker Track(this ILogger self, string type, object customData = null)
        {
            self.LogAsync(">>> " + type, customData);
            return new PerformanceTracker(self, type);
        }
    }
}