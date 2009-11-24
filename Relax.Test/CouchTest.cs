using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;

namespace Relax.Test
{
    public class CouchTest
    {
        private static Process _couchProcess;

        [TestFixtureSetUp]
        public virtual void __setup()
        {
            if (_couchProcess == null)
            {
                string path = String.Format(@"{0}\Apache Software Foundation\CouchDB\bin\couchdb.bat", ProgramFilesx86());
                Console.WriteLine("Path:" + path);
                ProcessStartInfo _Process = new ProcessStartInfo(path) { WorkingDirectory = Path.GetDirectoryName(path) };
                _couchProcess = Process.Start(_Process);
            }
        }
        [TestFixtureTearDown]
        public virtual void __teardown()
        {
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
