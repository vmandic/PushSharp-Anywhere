using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace PushSharp.Universal
{
    public class PushNotificationHelper
    {
        private PushNotificationChannel _channel;
        private bool _registeredSuccessfully;

        public PushNotificationChannel Channel
        {
            get
            {
                return this._channel;
            }
        }

        private Uri _3rdPartyWSUri;
        private Uri _pushAllMessageUri;
        private Uri _unregisterPN;
        // a hardcoded dummy client ID (that exists on the 3rd party DB) to simulate a real world app
        private readonly int _clientId = 1;
        private readonly string _mobileDeviceOS;
        private readonly string _mobileDeviceID;
        private string _registrationID;
        // your web service endpoint which will register your device on your DB to later push notifications to it
        private readonly string _registerUrlTemplate = "https://your.server.com/pushsharp/api/notification/device/register/{0}/{1}/{2}/{3}";
        private readonly string _pushAllMessageUrlTemplate = "https://your.server.com/pushsharp/api/notification/all/{0}/{1}";
        private readonly string _unregisterUrlTemplate = "https://your.server.com/pushsharp/api/notification/device/unregister/{0}";

        public PushNotificationHelper(string mobileDeviceOS)
        {
            this._mobileDeviceOS = mobileDeviceOS;
            this._mobileDeviceID = Utility.GetDeviceID();
        }


        /// <summary>
        /// Registers the current device for a push notification chennel, it will be passed to the 3rd party server.
        /// </summary>
        public async Task<bool> RegisterForWNS()
        {
            try
            {
                // a channel will live for 30 days
                _channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                Debug.WriteLine("Channel opened for URI: " + _channel.Uri.ToString());

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Unregisters you from the current 3rd party server for push notifications.
        /// </summary>
        public async Task<bool> UnregisterPushNotifications()
        {
            Exception exCatcher;

            try
            {
                _unregisterPN = new Uri(String.Format(_unregisterUrlTemplate, Utility.GetDeviceID()));

                var httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, _unregisterPN);
                request.Headers.Authorization = new HttpCredentialsHeaderValue("BASIC", "ZGV2dWc6cHVzaHNoYXJw");
                Debug.WriteLine("Sending a request for URL: " + _unregisterPN.AbsolutePath);

                // await the response from the 3rd party WS for unregistration status
                HttpResponseMessage response = await httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseContentRead);
                Debug.WriteLine("Response successfully received...");

                var content = await response.Content.ReadAsStringAsync();
                content = content.Trim('\"');

                bool respOK = response.IsSuccessStatusCode && !content.Contains("SERVER ERROR");

                if (respOK)
                {
                    await new MessageDialog("Successfully unregistered! :-)\n\n" + content).ShowAsync();
                    _registeredSuccessfully = false;
                }
                else
                    await new MessageDialog("Unregistration not successful! :-(\n\n" + content).ShowAsync();

                return respOK;
            }
            catch (Exception ex)
            {
                exCatcher = ex;
            }

            // in C# 6.0 you can await in catch :-) no need for this dirty hack!
            if (exCatcher != null)
                await new MessageDialog("Whoops! An error occured! :-(").ShowAsync();

            return false;
        }

        /// <summary>
        /// Registers you for push notifications on the third party server.
        /// </summary>
        public async Task<bool> RegisterChannelTo3rdPartyWS()
        {
            Exception exCatcher;

            try
            {
                if (_registeredSuccessfully)
                {
                    await new MessageDialog("Already registered! Restart for more... :-)").ShowAsync();
                    return true;
                }

                if (_channel != null && !String.IsNullOrEmpty(_channel.Uri))
                {
                    _registrationID = Utility.Base64Encode(_channel.Uri.ToString());

                    // WS endpoint (in our case): app.mobendo.com / pushsharp / api / notification / device / register / CID / RID / MDOS / DID
                    // build the endpoint URL; send a dummy client ID, channel registration ID, operating system ID and device ID
                    _3rdPartyWSUri = new Uri(String.Format
                    (
                        _registerUrlTemplate,
                        _clientId,
                        _registrationID,
                        _mobileDeviceOS,
                        _mobileDeviceID
                    ));

                    var httpClient = new HttpClient();
                    var request = new HttpRequestMessage(HttpMethod.Get, _3rdPartyWSUri);
                    request.Headers.Authorization = new HttpCredentialsHeaderValue("BASIC", "ZGV2dWc6cHVzaHNoYXJw");
                    Debug.WriteLine("Sending a request for URL: " + _3rdPartyWSUri.AbsolutePath);

                    // await the response from the 3rd party WS which will push the notifications later
                    HttpResponseMessage response = await httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseContentRead);
                    Debug.WriteLine("Response successfully received...");

                    var content = await response.Content.ReadAsStringAsync();
                    content = content.Trim('\"');

                    if (response.IsSuccessStatusCode && !content.Contains("SERVER ERROR"))
                    {
                        await new MessageDialog("Registered successfully to the 3rd party WS! :-)\n\n" + content).ShowAsync();
                        _registeredSuccessfully = true;
                    }
                    else
                    {
                        await new MessageDialog("Registered unsuccessfully to the 3rd party WS! :-(\n\n" + content).ShowAsync();
                        _registeredSuccessfully = false;
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                exCatcher = ex;
            }

            // in C# 6.0 you can await in catch :-) no need for this dirty hack!
            if (exCatcher != null)
                await new MessageDialog("Whoops! An error occured! :-(").ShowAsync();

            return false;
        }

        /// <summary>
        /// Pushes your message to everyone subscribed currently to the 3rd party push server. This is a broadcast action.
        /// </summary>
        /// <param name="message">The message you are broadcasting, stay polite... :-)</param>
        /// <param name="pushOrEnque">Decides wether you push or enque the message.</param>
        public async Task<bool> PushAllMessage(string message, int pushOrEnque)
        {
            if (!_registeredSuccessfully)
            {
                await new MessageDialog("Please register first for push notifications!").ShowAsync();
                return false;
            }

            Exception exCatcher;

            try
            {
                int isDirectPush = pushOrEnque;
                _pushAllMessageUri = new Uri(String.Format(_pushAllMessageUrlTemplate, message, isDirectPush));

                var httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, _pushAllMessageUri);
                request.Headers.Authorization = new HttpCredentialsHeaderValue("BASIC", "ZGV2dWc6cHVzaHNoYXJw");
                HttpResponseMessage response = await httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseContentRead);

                var content = await response.Content.ReadAsStringAsync();
                content = content.Trim('\"');

                if (response.IsSuccessStatusCode && !content.Contains("SERVER ERROR"))
                    await new MessageDialog("Success! :-)\n\n" + content).ShowAsync();
                else
                    await new MessageDialog("Failure! :-(\n\n" + content).ShowAsync();

                return true;
            }
            catch (Exception ex)
            {
                exCatcher = ex;
            }

            // in C# 6.0 you can await in catch :-) no need for this dirty hack!
            if (exCatcher != null)
                await new MessageDialog("Whoops! An error occured! :-(").ShowAsync();

            return false;
        }

    }
}
