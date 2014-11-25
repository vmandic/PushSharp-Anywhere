using PushSharp.CoreProcessor;
using System.ServiceProcess;

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
