using PushSharp.CoreProcessor;
using System;
using System.Windows.Forms;

namespace PushSharp.WinFormsNotifier
{
    public partial class NotifierForm : Form
    {
        private PushNotificationProcessor _processor;
        private const int LST_LIMIT = 100;

        public NotifierForm()
        {
            InitializeComponent();
            this._processor = new PushNotificationProcessor();

            this._processor.DisplayErrorMessage += _processor_DisplayErrorMessage;
            this._processor.DisplayMessage += _processor_DisplayMessage;
            this._processor.DisplayStatusMessage += _processor_DisplayStatusMessage;

            this.FormClosing += NotifierForm_FormClosing;
        }

        void _processor_DisplayStatusMessage(object sender, string message)
        {
            this.BeginInvoke(new Action(() =>
            {
                this.lblStatus.Text = String.Concat(DateTime.Now.ToString("HH:mm:ss"), " :: ", message);
            }));
        }

        void _processor_DisplayMessage(object sender, string message)
        {
            this.BeginInvoke(new Action(() =>
            {
                if (this.lstMessages.Items.Count > LST_LIMIT)
                    this.lstMessages.Items.Clear();

                this.lstMessages.Items.Insert(0, String.Concat(DateTime.Now.ToString("HH:mm:ss"), " :: ", message));
            }));
        }

        void _processor_DisplayErrorMessage(object sender, string message)
        {
            this.BeginInvoke(new Action(() =>
            {
                if (this.lstErrors.Items.Count > LST_LIMIT)
                    this.lstErrors.Items.Clear();

                this.lstErrors.Items.Insert(0, String.Concat(DateTime.Now.ToString("HH:mm:ss"), " :: ", message));
            }));
        }

        void NotifierForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this._processor.Stop();
        }

        private void btnStartPNP_Click(object sender, EventArgs e)
        {
            if (String.Equals(btnStartPNP.Text, "Running..."))
            {
                btnStartPNP.Text = "Start";
                btnStartPNP.Enabled = true;
                btnStopPNP.Text = "Stopped";
                btnStopPNP.Enabled = false;
            }
            else
            {
                this._processor.Start();
                btnStartPNP.Text = "Running...";
                btnStartPNP.Enabled = false;
                btnStopPNP.Text = "Stop";
                btnStopPNP.Enabled = true;
            }

        }

        private void btnStopPNP_Click(object sender, EventArgs e)
        {
            if (String.Equals(btnStopPNP.Text, "Stop"))
            {
                this._processor.Stop();
                btnStartPNP.Text = "Start";
                btnStartPNP.Enabled = true;
                btnStopPNP.Text = "Stopped";
                btnStopPNP.Enabled = false;
            }
            else
            {
                btnStartPNP.Text = "Running...";
                btnStartPNP.Enabled = false;
                btnStopPNP.Text = "Stop";
                btnStopPNP.Enabled = true;
            }

        }
    }
}
