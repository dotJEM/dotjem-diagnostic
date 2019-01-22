using System.Diagnostics;
using System.Security.Claims;
using System.Threading;

namespace DotJEM.Diagnostic.DataProviders
{
    public interface ICustomDataProvider
    {
        object Data { get; }
        string Format { get; }
    }
    public class ThreadIdProvider : ICustomDataProvider
    {
        public object Data => Thread.CurrentThread.ManagedThreadId;
        public string Format => "D";

    }
    public class ProcessIdProvider : ICustomDataProvider
    {
        public string Format => "D";
        public object Data => Process.GetCurrentProcess().Id;
    }

    public class IdentityProvider : ICustomDataProvider
    {
        public string Format => "";
        public object Data
        {
            get
            {
                string str = ClaimsPrincipal.Current.Identity.Name;
                return !string.IsNullOrEmpty(str) ? str : "NO IDENTITY";
            }
        }
    }

}