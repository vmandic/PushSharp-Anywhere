using PushSharp.CoreProcessor.Utility;
using PushSharp.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.Entity;

namespace PushSharp.WebAPI.Controllers
{
    [RoutePrefix("notification")]
    public class PushNotificationController : BaseApiController
    {
        private readonly string[] validDevices = new[] { "ios", "android", "wp8" };

        [HttpGet]
        [Route("new/{mdid:int:min(1)}/{m}/{direct:bool}")]
        public string New(int mdid, string m, bool direct = true)
        {
            try
            {
                if (String.IsNullOrEmpty(m) || m.Length <= 5)
                    throw new Exception("Message is too short! A message must have more then 5 characters.");

                var pushNotification = new PushNotification()
                {
                    CreatedAt = DateTime.Now,
                    Description = "(Web API) New message inserted.",
                    Message = m,
                    MobileDeviceID = mdid,
                    ModifiedAt = DateTime.Now,
                    Status = (int)PushNotificationStatus.Unprocessed
                };

                if (direct)
                {
                    if (!Processor.ProcessNotification(Context, true, pushNotification))
                        throw new Exception("Error on direct push of the notification!");
                }
                else
                {
                    Context.PushNotification.Add(pushNotification);
                    Context.SaveChanges();
                }

                return "Message successfully pushed at: " + DateTime.Now.ToString();
            }
            catch (Exception ex)
            {
                return String.Format("SERVER ERROR! Details: {0} Time: {1}", ex.Message, DateTime.Now.ToString());
            }
        }

        [HttpGet]
        [Route("device/register/{cid:int:min(1)/{rid}/{md}/{did}")]
        public string RegisterDevice(int cid, string rid, string md, string did)
        {
            try
            {
                if (String.Equals(rid.Trim(), "") || String.Equals(md.Trim(), "") || cid <= 0)
                    throw new Exception("Required parameters are invalid!");

                if (!validDevices.Contains(md))
                    throw new Exception("You're pushing a message for an invalid mobile devices OS! Valid OS are: ios, android and wp8.");

                var mobileDevice = new MobileDevice()
                {
                    Active = true,
                    ClientID = cid,
                    CreatedAt = DateTime.Now,
                    DeviceID = did,
                    ModifiedAt = DateTime.Now,
                    PushNotificationsRegistrationID = rid,
                    SmartphonePlatform = md
                };

                Context.MobileDevice.Add(mobileDevice);
                Context.SaveChanges();

                return "Device registered successfully at: " + DateTime.Now.ToString();
            }
            catch (Exception ex)
            {
                return String.Format("SERVER ERROR! Details: {0} Time: {1}", ex.Message, DateTime.Now.ToString());
            }
        }
    }
}
