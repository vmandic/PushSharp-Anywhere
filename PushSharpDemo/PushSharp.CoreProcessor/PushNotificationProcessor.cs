using Newtonsoft.Json;
using PushSharp.Android;
using PushSharp.Apple;
using PushSharp.Core;
using PushSharp.CoreProcessor.Utility;
using PushSharp.DataAccessLayer;
using PushSharp.WindowsPhone;
using System;
using System.Linq;
using System.Threading;
using Error = PushSharp.CoreProcessor.Utility.SimpleErrorLogger;

namespace PushSharp.CoreProcessor
{
    public class PushNotificationProcessor
    {
        // Fields
        private PushSharpDatabaseContext _dbContext;
        private Thread _processorThread;
        private bool _processorThreadRunning = false;
        private PushBroker _broker;
        private readonly string _googlePushNotificationAuthToken;
        private readonly byte[] _applePushNotificationCertificate;

        // Events
        public delegate void MessageHandler(object sender, string message);
        public event MessageHandler DisplayMessage, DisplayErrorMessage, DisplayStatusMessage;

        private void On(MessageHandler handler, string msg)
        {
            // check for subsriptions, if some, do some!
            if (handler != null)
                handler(this, msg);
        }

        // Constructor
        public PushNotificationProcessor(string appStartupPath = "")
        {
            _googlePushNotificationAuthToken = "";//"AIzaSyAhEKqp1ahCYQN7NB-PzQSB3G655xtyZEg";
            _applePushNotificationCertificate = new byte[] { };//File.ReadAllBytes("/Certificates/Apple-PushNotifications-DevCert.p12");

            _broker = new PushBroker();
            _dbContext = new PushSharpDatabaseContext();

            // subscribe to the push brokers API events
            _broker.OnNotificationSent += broker_OnNotificationSent;
            _broker.OnNotificationFailed += broker_OnNotificationFailed;
            _broker.OnServiceException += broker_OnServiceException;
            _broker.OnNotificationRequeue += broker_OnNotificationRequeue;
            _broker.OnDeviceSubscriptionExpired += broker_OnDeviceSubscriptionExpired;
            _broker.OnDeviceSubscriptionChanged += broker_OnDeviceSubscriptionChanged;
            _broker.OnChannelCreated += broker_OnChannelCreated;
            _broker.OnChannelDestroyed += broker_OnChannelDestroyed;
            _broker.OnChannelException += broker_OnChannelException;

            _broker.RegisterGcmService(new GcmPushChannelSettings(_googlePushNotificationAuthToken));
            //_broker.RegisterAppleService(new ApplePushChannelSettings(false, _applePushNotificationCertificate, "Password"));
            _broker.RegisterWindowsPhoneService();
        }

        public void Start()
        {
            On(DisplayMessage, "Notification processor starting...");
            _processorThread = new Thread(ProcessNotificationsLoop);
            _processorThread.Start();
            _processorThreadRunning = true;
        }

        public void Stop()
        {
            _processorThreadRunning = false;
            On(DisplayMessage, "Notification processor stopping...");
        }

        private void ProcessNotificationsLoop()
        {
            On(DisplayMessage, "Notification processor thread started.");

            // run a continuos loop for checking the database and its PushNotification entity
            while (_processorThreadRunning)
            {
                if (!ProcessNotification(_dbContext))
                {
                    On(DisplayStatusMessage, "No queued messages found, sleeping...");
                    // give it a rest, it's not that big of a rush...
                    Thread.Sleep(1000);
                }
            }

            _broker.StopAllServices();

            // unsubscribe from the API events
            //_broker.OnNotificationSent -= broker_OnNotificationSent;
            //_broker.OnNotificationFailed -= broker_OnNotificationFailed;
            //_broker.OnServiceException -= broker_OnServiceException;
            //_broker.OnNotificationRequeue -= broker_OnNotificationRequeue;
            //_broker.OnDeviceSubscriptionExpired -= broker_OnDeviceSubscriptionExpired;
            //_broker.OnDeviceSubscriptionChanged -= broker_OnDeviceSubscriptionChanged;
            //_broker.OnChannelCreated -= broker_OnChannelCreated;
            //_broker.OnChannelDestroyed -= broker_OnChannelDestroyed;
            //_broker.OnChannelException -= broker_OnChannelException;

            DisposeContext(_dbContext, true);

            On(DisplayMessage, "Notification processor thread stopped.");
            On(DisplayStatusMessage, "Idle.");
        }

        public bool ProcessNotification(PushSharpDatabaseContext ctx, bool disposeContext = false, PushNotification pushNotification = null)
        {
            On(DisplayMessage, "Checking for unprocessed notifications...");

            PushNotification notificationEntity = pushNotification;

            try
            {
                if (notificationEntity != null)
                    ctx.PushNotification.Add(pushNotification);
                else
                    notificationEntity = ctx.PushNotification.FirstOrDefault(s =>
                                    s.Status == (int)PushNotificationStatus.Unprocessed &&
                                    s.CreatedAt <= DateTime.Now);
            }
            catch (Exception ex)
            {
                On(DisplayErrorMessage, "EX. ERROR: Check for unprocessed notifications: " + ex.Message);
                SimpleErrorLogger.Log(ex);
            }

            if (notificationEntity != null)
            {
                On(DisplayStatusMessage, "Processing notification...");
                On(DisplayMessage, "ID " + notificationEntity.ID + " for " + notificationEntity.MobileDevice.Client.Username + " -> " + notificationEntity.Message);

                //---------------------------
                // ANDROID GCM NOTIFICATIONS
                //---------------------------
                if (notificationEntity.MobileDevice.SmartphonePlatform == "android")
                {
                    var gcmNotif = new GcmNotification() { Tag = notificationEntity.ID };
                    string msg = JsonConvert.SerializeObject(new { message = notificationEntity.Message });

                    gcmNotif.ForDeviceRegistrationId(notificationEntity.MobileDevice.PushNotificationsRegistrationID)
                            .WithJson(msg);

                    _broker.QueueNotification(gcmNotif);

                    notificationEntity.Status = (int)PushNotificationStatus.Processing;
                    notificationEntity.ModifiedAt = DateTime.Now;
                    notificationEntity.Description = "(Processor) Notification queued for sending.";
                }
                ////-------------------------
                //// APPLE iOS NOTIFICATIONS
                ////-------------------------
                else if (notificationEntity.MobileDevice.SmartphonePlatform == "ios")
                {
                    var appleNotif = new AppleNotification() { Tag = notificationEntity.ID };
                    var msg = new AppleNotificationPayload(notificationEntity.Message);

                    appleNotif.ForDeviceToken(notificationEntity.MobileDevice.PushNotificationsRegistrationID)
                                .WithPayload(msg)
                                .WithSound("default");

                    _broker.QueueNotification(appleNotif);

                    notificationEntity.Status = (int)PushNotificationStatus.Processing;
                    notificationEntity.ModifiedAt = DateTime.Now;
                    notificationEntity.Description = "(Processor) Notification queued for sending.";
                }
                //-----------------------------
                // WINDOWS PHONE 8 NOTIFICATIONS
                //-----------------------------
                else if (notificationEntity.MobileDevice.SmartphonePlatform == "wp8")
                {
                    var wpNotif = new WindowsPhoneToastNotification() { Tag = notificationEntity.ID };
                    wpNotif.ForEndpointUri(new Uri(notificationEntity.MobileDevice.PushNotificationsRegistrationID))
                        .ForOSVersion(WindowsPhoneDeviceOSVersion.Eight)
                        .WithBatchingInterval(BatchingInterval.Immediate)
                        .WithNavigatePath("/MainPage.xaml")
                        .WithText1("PushSharpDemo")
                        .WithText2(notificationEntity.Message);
                }
                else
                {
                    On(DisplayErrorMessage, "ERROR: Unsupported device OS: " + notificationEntity.MobileDevice.SmartphonePlatform);

                    notificationEntity.Status = (int)PushNotificationStatus.Error;
                    notificationEntity.ModifiedAt = DateTime.Now;
                    notificationEntity.Description = "(Processor) Unsupported device OS: " + notificationEntity.MobileDevice.SmartphonePlatform;
                }

                try
                {
                    _dbContext.SaveChanges();
                    return true;
                }
                catch (Exception ex)
                {
                    On(DisplayErrorMessage, "EX. ERROR: Updating notification, DB save failed: " + ex.Message);
                    Error.Log(ex);
                    return false;
                }
                finally
                {
                    DisposeContext(ctx, disposeContext);
                }
            }
            else
            {
                DisposeContext(ctx, disposeContext);
                // no messages were queued, take a nap...
                return false;
            }
        }

        private void DisposeContext(PushSharpDatabaseContext ctx, bool disposeContext)
        {
            if (disposeContext)
            {
                ctx.Dispose();
                On(DisplayMessage, "Database context disposed.");
            }
        }

        private void broker_OnChannelException(object sender, IPushChannel pushChannel, Exception error)
        {
            throw new NotImplementedException();
        }

        private void broker_OnChannelDestroyed(object sender)
        {
            throw new NotImplementedException();
        }

        private void broker_OnChannelCreated(object sender, IPushChannel pushChannel)
        {
            throw new NotImplementedException();
        }

        private void broker_OnDeviceSubscriptionChanged(object sender, string oldSubscriptionId, string newSubscriptionId, INotification notification)
        {
            throw new NotImplementedException();
        }

        private void broker_OnDeviceSubscriptionExpired(object sender, string expiredSubscriptionId, DateTime expirationDateUtc, INotification notification)
        {
            throw new NotImplementedException();
        }

        private void broker_OnNotificationRequeue(object sender, NotificationRequeueEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void broker_OnServiceException(object sender, Exception error)
        {
            throw new NotImplementedException();
        }

        private void broker_OnNotificationFailed(object sender, INotification notification, Exception error)
        {
            throw new NotImplementedException();
        }

        private void broker_OnNotificationSent(object sender, INotification notification)
        {
            try
            {
                int ID = Convert.ToInt32(notification.Tag);

                var notif = _dbContext.PushNotification.FirstOrDefault(s => s.ID == ID);
                if (notif != null)
                {
                    On(DisplayMessage, "ID " + notif.ID + " sent.");

                    notif.Description = "(Processor) Notification sent.";
                    notif.Status = (int)PushNotificationStatus.Processed;

                    _dbContext.SaveChanges();
                }
                else
                {
                    On(DisplayErrorMessage, "ERROR: Notification " + notif.ID + " not found in database");
                }
            }
            catch (Exception ex)
            {
                On(DisplayErrorMessage, "EX. ERROR: Get notification from DB: " + ex.Message);
                Error.Log(ex);
            }
        }
    }
}
