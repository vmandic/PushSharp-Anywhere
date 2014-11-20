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

        private Uri _requestFor3rdPartyWS;
        // a hardcoded dummy client ID (that exists on the 3rd party DB) to simulate a real world app
        private readonly int _clientId = 1; 
        private readonly string _mobileDeviceOS;
        private readonly string _mobileDeviceID;
        private string _registrationID;

        public PushNotificationHelper(string mobileDeviceOS)
        {
            this._mobileDeviceOS = mobileDeviceOS;
            this._mobileDeviceID = Utility.GetDeviceID();
        }

        public async Task<bool> RegisterForWNS() 
        {
            try
            {
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

                    // WS endpoint: app.mobendo.com / pushsharp / api / notification / device / register / CID / RID / MDOS / DID
                    // build the endpoint URL; send a dummy client ID, channel registration ID, operating system ID and device ID
                    _requestFor3rdPartyWS = new Uri(String.Format
                    (
                        "https://app.mobendo.com/pushsharp/api/notification/device/register/{0}/{1}/{2}/{3}",
                        _clientId,
                        _registrationID,
                        _mobileDeviceOS,
                        _mobileDeviceID
                    ));

                    var httpClient = new HttpClient();
                    var request = new HttpRequestMessage(HttpMethod.Get, _requestFor3rdPartyWS);
                    request.Headers.Authorization = new HttpCredentialsHeaderValue("BASIC", "ZGV2dWc6cHVzaHNoYXJw");
                    Debug.WriteLine("Sending a request for URL: " + _requestFor3rdPartyWS.AbsolutePath);

                    // await the response from the 3rd party WS which will push the notifications later
                    HttpResponseMessage response = await httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseContentRead);
                    Debug.WriteLine("Response successfully received...");

                    var content = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode && !content.Contains("SERVER ERROR"))
                    {
                        await new MessageDialog("Registered successfully to the 3rd party WS! :-)").ShowAsync();
                        _registeredSuccessfully = true;
                    }
                    else
                    {
                        await new MessageDialog("Registered unsuccessfully to the 3rd party WS! :-(\n\n" + content.TrimStart('\"').TrimEnd('\"')).ShowAsync();
                        _registeredSuccessfully = false;
                    }

                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        
        }
    }
}
