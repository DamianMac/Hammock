using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;

namespace RedBranch.Hammock
{
    public static class CouchProcess
    {
        private static bool isInDebugMode = false;
        private static bool initialized = false;
     
        public static void EnsureRunning(int port)
        {
#if DEBUG
            isInDebugMode = true;
#endif
            try
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        EnsureRunningUnix(port);
                        return;

                    case PlatformID.Win32Windows:
                    case PlatformID.Win32NT:
                        EnsureRunningWindows(port);
                        return;
                }
            }
            catch
            {
            }
            throw new NotSupportedException("This method is not supported on the current operating system.");
        }

        static Process EnsureRunningUnix(int port)
        {
            return null;
        }

        private static bool IsAlreadyRunningWindows(int port)
        {
            var tcpConnInfoArray = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            bool result = tcpConnInfoArray.Where(x => x.Port == port).Count() == 1;
            return result;
        }
        static void EnsureRunningWindows(int port)
        {
            if (!initialized)
            {
                if (IsAlreadyRunningWindows(port))
                {
                    initialized = true;
                    return;
                }
                var path = String.Format(@"{0}\Apache Software Foundation\CouchDB\bin\couchdb.bat", ProgramFilesx86());
                if (File.Exists(path))
                {
                    var psi = new ProcessStartInfo(path)
                    {
                        CreateNoWindow = !isInDebugMode,
                        WindowStyle = isInDebugMode ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                        WorkingDirectory = Path.GetDirectoryName(path)
                    };
                    Process.Start(psi);
                    initialized = true;
                }
                else
                {
                    throw new FileNotFoundException("CouchDB not installed");
                }
            }
        }

        static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }
    }
}
