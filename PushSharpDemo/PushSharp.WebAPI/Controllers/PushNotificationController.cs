using PushSharp.CoreProcessor.Utility;
using PushSharp.DataAccessLayer;
using PushSharp.WebAPI.Filters;
using PushSharp.WebAPI.Utility;
using System;
using System.Linq;
using System.Web.Http;

namespace PushSharp.WebAPI.Controllers
{
    [CustomAuthorizationFilter]
    [RoutePrefix("api/notification")]
    public class PushNotificationController : BaseApiController
    {

        public PushNotificationController()
        {

        }

        [HttpGet]
        [Route("index")]
        public string Index()
        {
            return "Welcome to the PushSharp Push Notifications Web Service.";
        }

        /// <summary>
        /// Pushes a new message to the push notification subscribers.
        /// </summary>
        /// <param name="mdid">Mobile Device ID.</param>
        /// <param name="m">Mesage text.</param>
        /// <param name="direct">Decides wethere the message will be queued in the database or pushed directly, pushes directly if true, else queues to the DB.</param>
        /// <returns>A string information about the sent push message.</returns>
        [HttpGet]
        [Route("new/{mdid:int:min(1)}/{m}/{direct:int:max(1)}")]
        public string New(int mdid, string m, int direct)
        {
            try
            {
                if (String.IsNullOrEmpty(m) || m.Length <= 5 || m.Length > 60)
                    throw new Exception("The message must be between 5 and 60 characters!");

                var pushNotification = new PushNotification()
                {
                    CreatedAt = DateTime.Now,
                    Message = m,
                    MobileDeviceID = mdid,
                    ModifiedAt = DateTime.Now,
                    Status = (int)PushNotificationStatus.Unprocessed
                };

                if (direct == 1)
                {
                    pushNotification.Description = "(Web API) New message pushed directly.";

                    // if true, the message will get processed and saved to the DB, dbctx will be disposed
                    if (!Processor.ProcessNotification(DatabaseContext, true, pushNotification))
                        throw new Exception("Error on direct push of the notification!");
                    else
                        return "Message successfully queued at: " + DateTime.Now.ToString();
                }
                else
                {
                    pushNotification.Description = "(Web API) New message queued for push.";

                    // queue for push, processor will handle
                    DatabaseContext.PushNotification.Add(pushNotification);
                    DatabaseContext.SaveChanges();
                    DatabaseContext.Dispose();

                    return "Message successfully queued at: " + DateTime.Now.ToString();
                }


            }
            catch (Exception ex)
            {
                return String.Format("SERVER ERROR! Details: {0} Time: {1}", ex.Message, DateTime.Now.ToString());
            }
        }

        /// <summary>
        /// Subscribes the mobile device for push notifications.
        /// </summary>
        /// <param name="cid">Client ID.</param>
        /// <param name="rid">Registration string ID.</param>
        /// <param name="mdos">Mobile device operating system. The valids are "ios", "android" and "wp8".</param>
        /// <param name="did">Mobile device platform string ID.</param>
        /// <returns>A message informing about the device registration status.</returns>
        [HttpGet]
        [Route("device/register/{cid:int:min(1)}/{rid}/{mdos}/{did}")]
        public string RegisterDevice(int cid, string rid, string mdos, string did)
        {
            bool isNewEntity = false;

            try
            {
                if (String.Equals(rid.Trim(), "") || String.Equals(mdos.Trim(), "") || cid <= 0)
                    throw new Exception("Required parameters are invalid!");

                if (!_validDevices.Contains(mdos))
                    throw new Exception("You're pushing a message for an invalid mobile devices OS! Valid OS are: ios, android and wp8.");

                // decode the registration channel
                if (mdos == "wp8")
                    rid = Helpers.DecodeBase64NTimes(rid, 1);

                MobileDevice mobileDevice = DatabaseContext.MobileDevice.FirstOrDefault(x => x.ClientID == cid && x.DeviceID.Equals(did));

                isNewEntity = mobileDevice == null;
                mobileDevice = mobileDevice ?? new MobileDevice();

                mobileDevice.ModifiedAt = DateTime.Now;
                mobileDevice.PushNotificationsRegistrationID = rid;

                if (isNewEntity)
                {
                    mobileDevice.ClientID = cid;
                    mobileDevice.DeviceID = did;
                    mobileDevice.Active = true;
                    mobileDevice.CreatedAt = DateTime.Now;
                    mobileDevice.SmartphonePlatform = mdos;
                    DatabaseContext.MobileDevice.Add(mobileDevice);
                }

                DatabaseContext.SaveChanges();
                DatabaseContext.Dispose();

                return String.Format("Device {0} successfully at: {1}, MobileDeviceID: {2}", isNewEntity ? "registered" : "updated", DateTime.Now.ToString(), mobileDevice.ID);
            }
            catch (Exception ex)
            {
                return String.Format("SERVER ERROR! Details: {0} Time: {1}", ex.Message, DateTime.Now.ToString());
            }
        }
    }
}
