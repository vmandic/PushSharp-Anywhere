using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            await HandleRegistration();
        }

        private async System.Threading.Tasks.Task HandleRegistration()
        {
            if (await _pushNotificationHelper.RegisterForWNS() && await _pushNotificationHelper.RegisterChannelTo3rdPartyWS())
            {
                // registration successfull, subscribe for notifiactions received
                _pushNotificationHelper.Channel.PushNotificationReceived += async (sender, pushNotificationReceivedEventArgs) =>
                {
                    await new MessageDialog("A toast was received!", "Toast!").ShowAsync();
                };
            }
        }

        private async void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            await HandleRegistration();
        }
    }
}
