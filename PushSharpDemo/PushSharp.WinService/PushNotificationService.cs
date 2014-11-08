using PushSharp.CoreProcessor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PushSharp.WinService
{
    public partial class PushNotificationService : ServiceBase
    {
        private PushNotificationProcessor _processor;

        public PushNotificationService()
        {
            InitializeComponent();
            this._processor = new PushNotificationProcessor();
        }

        protected override void OnStart(string[] args)
        {
            this._processor.Start();
        }

        protected override void OnStop()
        {
            this._processor.Stop();
        }
    }
}
