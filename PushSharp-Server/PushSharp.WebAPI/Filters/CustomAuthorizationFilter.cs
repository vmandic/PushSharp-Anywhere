using PushSharp.CoreProcessor.Utility;
using PushSharp.WebAPI.Utility;
using System;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace PushSharp.WebAPI.Filters
{
    public class CustomAuthorizationFilter : System.Web.Http.AuthorizeAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var clientAddress = actionContext.Request.GetClientIpAddress();

            if (clientAddress != "::1" && !Authorize(actionContext))
                HandleUnauthorizedRequest(actionContext);

            //Check if HTTPS is being used when LAN device/localhost is not the requester, i.e. HTTP is valid on LAN/localhost
            if (!clientAddress.Contains("192.168.1") && !clientAddress.Contains("::1") && actionContext.Request.RequestUri.Scheme != Uri.UriSchemeHttps)
            {
                actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
                {
                    ReasonPhrase = "HTTPS is required!",
                    Content = new StringContent("HTTPS is required!")
                };
            }
        }

        /// <summary>
        /// Handles an unauthorized request, sends an apropriate response message back to the request sender.
        /// </summary>
        /// <param name="actionContext">Request context.</param>
        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            var challengeMessage = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("API request unauthorized!")
            };

            challengeMessage.Headers.Add("WWW-Authenticate", "BASIC");
            throw new HttpResponseException(challengeMessage);
        }

        /// <summary>
        /// Handles authorization by checking the api key from the basic authorization header.
        /// </summary>
        /// <param name="actionContext">Request context.</param>
        /// <returns>True if valid, else false.</returns>
        private bool Authorize(HttpActionContext actionContext)
        {
            try
            {
                if (actionContext.Request.Headers.Authorization == null || actionContext.Request.Headers.Authorization.Scheme.ToLower() != "basic")
                    return false;

                string authEncoded = actionContext.Request.Headers.Authorization.Parameter ?? "";
                string[] credentials = !authEncoded.Equals("") ? Helpers.DecodeBase64NTimes(authEncoded, 1).Split(':') : new string[] { " ", " " };

                //validate the apikey from the basic auth key pair
                return credentials[0] == "devug" && credentials[1] == "pushsharp";
            }
            catch (Exception ex)
            {
                SimpleErrorLogger.Log(ex);
                return false;
            }
        }
    }
}