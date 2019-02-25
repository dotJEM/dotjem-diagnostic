using System;
using System.Runtime.InteropServices;

namespace DotJEM.Diagnostic.Common
{
    public class HighResolutionTime
    {
        [DllImport("Kernel32", CallingConvention = CallingConvention.Winapi)]
        static extern void GetSystemTimePreciseAsFileTime(out long fileTime);

        //TODO: Better design which selects a strategy
        private static readonly Lazy<bool> isWindows = new Lazy<bool>(() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        public static DateTime Now
        {
            get
            {
                if(isWindows.Value)
                {
                    //TODO: Investigate how this should be implemented with .NET Core on Linux/OSX in mind, seems like DateTime.UtcNow does the same thing but concern would
                    //      Be that since it doesn't do that for .NET 4.X we loose precision here.
                    GetSystemTimePreciseAsFileTime(out var fileTime);
                    return DateTime.FromFileTime(fileTime).ToUniversalTime();
                }
                return DateTime.UtcNow;
            }
        }
    }
}