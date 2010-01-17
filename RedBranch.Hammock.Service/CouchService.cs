using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace RedBranch.Hammock.Service
{
    public partial class CouchService : ServiceBase
    {
        private Process _process;

        public CouchService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _process = CouchProcess.EnsureRunning(new Uri("http://localhost:5984"));
        }

        protected override void OnStop()
        {
            if (null != _process)
            {
                _process.Kill();
                _process = null;
            }
        }
    }
}
