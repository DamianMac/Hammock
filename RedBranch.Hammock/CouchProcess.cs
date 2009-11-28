using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RedBranch.Hammock
{
    public static class CouchProcess
    {
        static Process _couchProcess;

        public static Process EnsureRunning()
        {
            try
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        return EnsureRunningUnix();

                    case PlatformID.Win32Windows:
                    case PlatformID.Win32NT:
                        return EnsureRunningWindows();
                }
            }
            catch
            {
                return null;
            }
            throw new NotSupportedException("This method is not supported on the current operating system.");
        }

        static Process EnsureRunningUnix()
        {
            return null;
        }

        static Process EnsureRunningWindows()
        {
            if (_couchProcess == null)
            {
                var path = String.Format(@"{0}\Apache Software Foundation\CouchDB\bin\couchdb.bat", ProgramFilesx86());
                if (File.Exists(path))
                {
                    var psi = new ProcessStartInfo(path) {WorkingDirectory = Path.GetDirectoryName(path)};
                    _couchProcess = Process.Start(psi);
                }
                else
                {
                    return null;
                }
            }
            return _couchProcess;
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
