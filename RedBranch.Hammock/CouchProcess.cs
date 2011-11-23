using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;

namespace RedBranch.Hammock
{
    public class CouchProcess
    {
        public Uri Location { get; private set; }
		
		private bool Initialized { get; set; }
		
		public CouchProcess(Uri location)
		{
		    Location = location;
		    if (!Location.IsLoopback) 
			{
		        throw new NotSupportedException("CouchProcess only supports loopback connections (localhost, 127.0.0.1, etc).");
		    }
		}
		
		public Connection Connect()
		{
			Initialize();
			return new Connection(Location);
		}
		
		private void Initialize()
		{
			if (!Initialized)
			{
				EnsureRunning(Location);
				Initialized = true;
			}
		}
		
        public static Process EnsureRunning(Uri location)
        {
            switch (Environment.OSVersion.Platform) 
            {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    return EnsureRunningUnix(location.Port);
 
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                    return EnsureRunningWindows(location.Port);
            }
            throw new NotSupportedException("Unsupported Operating System.");
        }

        static Process EnsureRunningUnix(int port)
        {
            return null;
        }

        private static bool IsAlreadyRunningWindows(int port)
        {
            return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(x => x.Port == port);
        }
		
        private static Process EnsureRunningWindows(int port)
        {
            if (IsAlreadyRunningWindows(port))
            {
                return null;
            }
            var path = String.Format(@"{0}\Apache Software Foundation\CouchDB\bin\couchdb.bat", GetProgramFilesx86());
            if (File.Exists(path))
            {
                var psi = new ProcessStartInfo(path)
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(path)
                };
                return Process.Start(psi);
            }
			return null;
        }

        private static string GetProgramFilesx86()
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