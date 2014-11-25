using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace PushSharp.Universal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private PushNotificationHelper _pushNotificationHelper;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;

            #if WINDOWS_PHONE_APP
                this._pushNotificationHelper = new PushNotificationHelper("wp");
            #else
                this._pushNotificationHelper = new PushNotificationHelper("wsa");
            #endif
        }

        private async void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            ButtonsEnabled(false);

            if (await _pushNotificationHelper.RegisterForWNS())
                await _pushNotificationHelper.RegisterChannelTo3rdPartyWS();

            ButtonsEnabled();
        }

        private async void btnPushAll_Click(object sender, RoutedEventArgs e)
        {
            await HandlePushOrEnque(1);
        }

        private async Task HandlePushOrEnque(int pushOrEnque)
        {
            ButtonsEnabled(false);

            if (!String.IsNullOrEmpty(tbSpeakUp.Text) && tbSpeakUp.Text.Length > 5)
                await _pushNotificationHelper.PushAllMessage(tbSpeakUp.Text, pushOrEnque);
            else
                await new MessageDialog("Please more then five characters... and don't spam it please... :)").ShowAsync();

            ButtonsEnabled();
        }

        private void ButtonsEnabled(bool isEnabled = true)
        {
            btnPushAll.IsEnabled = btnRegister.IsEnabled = btnUnregister.IsEnabled = btnEnqueue.IsEnabled = isEnabled;
        }

        private async void btnUnregister_Click(object sender, RoutedEventArgs e)
        {
            ButtonsEnabled(false);
            await _pushNotificationHelper.UnregisterPushNotifications();
            ButtonsEnabled();
        }

        private async void btnEnqueue_Click(object sender, RoutedEventArgs e)
        {
            await HandlePushOrEnque(0);
        }
    }
}
