using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Diagnostic.Model
{
    public class TraceEvent
    {
        public string Type { get; }
        public DateTime Time { get; }
        public string Correlation { get; }
        public CustomData[] CustomData { get; }
        public JToken CustomObject { get; }

        private readonly FormattableString toStringImpl;

        public TraceEvent(string type, DateTime time, string correlation, IEnumerable<CustomData> customData, JToken customObject)
        {
            Type = type;
            Time = time;
            Correlation = correlation;
            CustomData = customData.ToArray();
            CustomObject = customObject;
            toStringImpl = SelectToStringImplementation(CustomData, CustomObject);
        }

        private FormattableString SelectToStringImplementation(CustomData[] customData, JToken customObject)
        {
            switch (customData.Length)
            {
                case int n when (n == 0 && customObject == null):
                    return $"{Time:yyyy-MM-ddTHH:mm:ss.fffffff}\t{Correlation}\t{Type}";
                case int n when (n == 0):
                    return $"{Time:yyyy-MM-ddTHH:mm:ss.fffffff}\t{Correlation}\t{Type}\t{CustomObject?.ToString(Formatting.None)}";
                case int n when (n == 1 && customObject == null):
                    return $"{Time:yyyy-MM-ddTHH:mm:ss.fffffff}\t{Correlation}\t{Type}\t{CustomData[0]}";
                case int n when (n == 1):
                    return $"{Time:yyyy-MM-ddTHH:mm:ss.fffffff}\t{Correlation}\t{Type}\t{CustomData[0]}\t{CustomObject?.ToString(Formatting.None)}";
                case int _ when (customObject == null):
                    return $"{Time:yyyy-MM-ddTHH:mm:ss.fffffff}\t{Correlation}\t{Type}\t{string.Join("\t", CustomData.Select(data => data.ToString()))}";
                default:
                    return $"{Time:yyyy-MM-ddTHH:mm:ss.fffffff}\t{Correlation}\t{Type}\t{string.Join("\t", CustomData.Select(data => data.ToString()))}\t{CustomObject?.ToString(Formatting.None)}";
            }
        }


        /*
         * Log as:
         *
         * Time                            Id                         Thread     CustomFields        CustomData
         * 2018-11-30T11:16:52.657862      02efb36.b992e58.0000000    47         JMD     >>>         { }
         * 2018-11-30T11:16:53.657862      02efb36.fe92a58.b992e58    134        JMD     >>>         { }
         * 2018-11-30T11:16:54.657862      02efb36.e452fe8.fe92a58    200        JMD     >>>         { }
         * 2018-11-30T11:16:54.757862      02efb36.aef928a.e452fe8    42         JMD     <<<         { }
         * 2018-11-30T11:16:56.657862      02efb36.a129ee8.fe92a58    9          JMD     >>>         { }
         *
         * Custom fields are custom provider for fixed length fields, this can be identity of the user, start/stop signalling etc
         * 
         * CustomData is a custom data object, which is ToString'ed... Each aditional line will be prepended with a indentation to
         * identify lines belonging to the statement. CustomData is merely a Custom field provider
         *
         *
         */
        public override string ToString()
            => toStringImpl.ToString();


    }
}