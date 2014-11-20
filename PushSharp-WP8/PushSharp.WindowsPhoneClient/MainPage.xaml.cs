using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.PushNotifications;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using System.Windows;
using Windows.UI.Popups;
using System.Text;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace PushSharp.WindowsPhoneClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private PushNotificationChannel channel = null;
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        void chnl_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            if (args.NotificationType == PushNotificationType.Toast)
            {
                channel.PushNotificationReceived += chnl_PushNotificationReceived;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                //Windows Server registration
                channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();

                Debug.WriteLine("Channel URI: " + channel.Uri.ToString());

                int clientID = 1;
                String registrationID = channel.Uri.ToString();
                String registrationIDEncoded = EncodeUrl(registrationID);
                String mobileDeviceOS = "wp8";
                String mobileDeviceID = GetDeviceID();

                //WebService string: app.mobendo.com / pushsharp / api / notification / device / register / CID / RID / MDOS / DID
                String url = String.Format("https://app.mobendo.com/pushsharp/api/notification/device/register/{0}/{1}/{2}/{3}"
                    , clientID
                    , registrationIDEncoded
                    , mobileDeviceOS
                    , mobileDeviceID);

                Windows.Web.Http.HttpClient oHttpClient = new Windows.Web.Http.HttpClient();
                Uri uri = new Uri(url);

                HttpRequestMessage mSent = new HttpRequestMessage(HttpMethod.Get, uri);
                mSent.Headers.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("BASIC", "ZGV2dWc6cHVzaHNoYXJw");

                HttpResponseMessage mReceived = await oHttpClient.SendRequestAsync(mSent, HttpCompletionOption.ResponseContentRead);

                if (mReceived.IsSuccessStatusCode)
                {
                    MessageDialog messageDialog = new MessageDialog("Register successful!");
                    await messageDialog.ShowAsync();
                }
                else
                {
                    MessageDialog messageDialog = new MessageDialog("Register unsuccessful!");
                    await messageDialog.ShowAsync();
                }

            }

            catch (Exception ex)
            {
                // Could not create a channel. 
            }
        }

        private String GetDeviceID()
        {
            HardwareToken token = HardwareIdentification.GetPackageSpecificToken(null);
            IBuffer hardwareId = token.Id;

            byte[] hwIDBytes = hardwareId.ToArray();

            String deviceID = hwIDBytes.Select(b => b.ToString()).Aggregate((b, next) => b + next);

            return deviceID;
        }

        private String EncodeUrl(String input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var base64 = Convert.ToBase64String(bytes);
            return base64;
        }
    }
}
