using System.Diagnostics;
using System.Security.Claims;
using System.Threading;
using DotJEM.Diagnostic.Model;

namespace DotJEM.Diagnostic.DataProviders
{
    public interface ICustomDataProvider
    {
        CustomData Generate(string key);
    }

    public class ThreadIdProvider : ICustomDataProvider, ICustomDataFormatter
    {
        public CustomData Generate(string key) => new CustomData(Thread.CurrentThread.ManagedThreadId, this);

        public string Format(CustomData customData) => $"ThreadId:{customData.Value:D}";
    }
    public class ProcessIdProvider : ICustomDataProvider, ICustomDataFormatter
    {
        public CustomData Generate(string key) => new CustomData(Process.GetCurrentProcess().Id, this);

        public string Format(CustomData customData) => $"ProcessId:{customData.Value:D}";
    }

    public class IdentityProvider : ICustomDataProvider
    {
        private string GetIdentityString()
        {
            string str = ClaimsPrincipal.Current.Identity.Name;
            return !string.IsNullOrEmpty(str) ? str : "NO IDENTITY";
        }

        public CustomData Generate(string key) => new CustomData(GetIdentityString());
    }

}