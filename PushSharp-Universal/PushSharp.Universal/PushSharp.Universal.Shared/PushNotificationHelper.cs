using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.UI.Popups;
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
        // a hardcoded dummy client ID (that exists on the 3rd party DB) to simulate a real world app
        private readonly int _clientId = 1; 
        private readonly string _mobileDeviceOS;
        private readonly string _mobileDeviceID;
        private string _registrationID;
        // your web service endpoint which will register your device on your DB to later push notifications to it
        private readonly string _registerUrlTemplate = "https://app.mobendo.com/pushsharp/api/notification/device/register/{0}/{1}/{2}/{3}";
        private readonly string _pushAllMessageUrlTemplate = "https://app.mobendo.com/pushsharp/api/notification/all/{0}";

        public PushNotificationHelper(string mobileDeviceOS)
        {
            this._mobileDeviceOS = mobileDeviceOS;
            this._mobileDeviceID = Utility.GetDeviceID();
        }

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

        public async Task<bool> PushAllMessage(string message)
        {
            Exception exCatcher;

            try
            {
                _pushAllMessageUri = new Uri(String.Format(_pushAllMessageUrlTemplate, message));

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
