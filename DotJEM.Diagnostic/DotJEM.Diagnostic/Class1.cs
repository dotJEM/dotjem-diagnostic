using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DotJEM.Diagnostic.Correlation;

namespace DotJEM.Diagnostic
{
    /*
     * Log as:
     *
     * Time                            Id                         Thread     CustomFields        Data
     * 2018-11-30T11:16:52.657862      02efb36.b992e58.0000000    47         JMD     >>>         { }
     * 2018-11-30T11:16:53.657862      02efb36.fe92a58.b992e58    134        JMD     >>>         { }
     * 2018-11-30T11:16:54.657862      02efb36.e452fe8.fe92a58    200        JMD     >>>         { }
     * 2018-11-30T11:16:54.757862      02efb36.aef928a.e452fe8    42         JMD     <<<         { }
     * 2018-11-30T11:16:56.657862      02efb36.a129ee8.fe92a58    9          JMD     >>>         { }
     *
     * Custom fields are custom provider for fixed length fields, this can be identity of the user, start/stop signalling etc
     * 
     * Data is a custom data object, which is ToString'ed... Each aditional line will be prepended with a indentation to
     * identify lines belonging to the statement. Data is merely a Custom field provider
     *
     *
     */

    //Fixed Data Provider!
    public interface ITraceDataProvider
    {
        
    }

    public abstract class TraceEvent
    {
        public DateTimeOffset Time { get; } = HighResolutionDateTime.Now;
        public string Correlation { get; } = CorrelationScope.Current?.ToString() ?? "00000000.00000000.00000000";
        //TODO: Consider these as custom data instead...
        public int ThreadId { get; } = Thread.CurrentThread.ManagedThreadId;
        public int ProcessId { get; } = Process.GetCurrentProcess().Id;
        public object[] Data { get; }

        public override string ToString() => $"{Time:O}\t{Correlation}\t{ThreadId:D5}\t{ProcessId:D5}\t{string.Join("\t", Data)}";
    }


}
