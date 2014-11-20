using Newtonsoft.Json;
using PushSharp.Android;
using PushSharp.Apple;
using PushSharp.Core;
using PushSharp.CoreProcessor.Utility;
using PushSharp.DataAccessLayer;
//using PushSharp.WindowsPhone;
using PushSharp.Windows;
using System;
using System.Linq;
using System.Threading;
using System.Data.Entity;
using Error = PushSharp.CoreProcessor.Utility.SimpleErrorLogger;

namespace PushSharp.CoreProcessor
{
    public class PushNotificationProcessor
    {
        // Fields
        private PushSharpDatabaseContext _databaseContext;
        private Thread _processorThread;
        private bool _processorThreadRunning = false;
        private bool _isDirectPush = false;
        private PushBroker _broker;
        private readonly string _googlePushNotificationAuthToken;
        private readonly byte[] _applePushNotificationCertificate;
        private readonly WindowsPushChannelSettings _windowsPushNotificationChannelSettings;
        //no certs/keys for windows phone 8

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
            // REQUIRED PLATFORM AUTHORIZATION TOKENS: 
            // register for those at: https://developer.apple.com/ and https://developers.google.com/

            _googlePushNotificationAuthToken = "";//"AIzaSyAhEKqp1ahCYQN7NB-PzQSB3G655xtyZEg";
            _applePushNotificationCertificate = new byte[] { };//File.ReadAllBytes("/Certificates/Apple-PushNotifications-DevCert.p12");

            // TODO: register for windows...
            _windowsPushNotificationChannelSettings = new WindowsPushChannelSettings("todo", "todo", "todo");
        }

        private void InitBroker()
        {
            _broker = new PushBroker();
            _databaseContext = new PushSharpDatabaseContext();

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

            //_broker.RegisterGcmService(new GcmPushChannelSettings(_googlePushNotificationAuthToken));
            //_broker.RegisterAppleService(new ApplePushChannelSettings(false, _applePushNotificationCertificate, "Password"));
            _broker.RegisterWindowsService(_windowsPushNotificationChannelSettings);

            On(DisplayMessage, "Push Broker successfully initialized.");
        }

        private void KillBroker(PushSharpDatabaseContext dbContext)
        {
            _broker.StopAllServices();

            // unsubscribe from the API events
            _broker.OnNotificationSent -= broker_OnNotificationSent;
            _broker.OnNotificationFailed -= broker_OnNotificationFailed;
            _broker.OnServiceException -= broker_OnServiceException;
            _broker.OnNotificationRequeue -= broker_OnNotificationRequeue;
            _broker.OnDeviceSubscriptionExpired -= broker_OnDeviceSubscriptionExpired;
            _broker.OnDeviceSubscriptionChanged -= broker_OnDeviceSubscriptionChanged;
            _broker.OnChannelCreated -= broker_OnChannelCreated;
            _broker.OnChannelDestroyed -= broker_OnChannelDestroyed;
            _broker.OnChannelException -= broker_OnChannelException;

            DisposeContext(dbContext, true);

            On(DisplayMessage, "Push Broker successfully stopped.");
        }

        public void Start()
        {
            if (!_processorThreadRunning)
            {
                On(DisplayMessage, "Notification processor starting...");

                InitBroker();

                _processorThread = new Thread(ProcessNotificationsLoop);
                _processorThread.Start();
                _processorThreadRunning = true;
            }
            else
                On(DisplayErrorMessage, "Can not start processor! Processor already running.");
        }

        public void Stop()
        {
            if (_processorThreadRunning)
            {
                _processorThreadRunning = false;

                KillBroker(_databaseContext);
                On(DisplayMessage, "Notification processor stopping...");
            }
            else
                On(DisplayErrorMessage, "Can not stop processor! Processor already stopped.");
        }

        private void ProcessNotificationsLoop()
        {
            On(DisplayMessage, "Notification processor thread started.");

            // run a continuos loop for checking the database and its PushNotification entity
            while (_processorThreadRunning)
            {
                if (!ProcessNotification(_databaseContext))
                {
                    On(DisplayStatusMessage, "No queued messages found, sleeping...");
                    // give it a rest, it's not that big of a rush...
                    Thread.Sleep(1000);
                }
            }

            On(DisplayMessage, "Notification processor thread stopped.");
            On(DisplayStatusMessage, "Idle.");
        }

        public bool ProcessNotification(PushSharpDatabaseContext databaseContext, bool isDirectPush = false, PushNotification pushNotification = null)
        {
            _isDirectPush = isDirectPush;
            _databaseContext = databaseContext;

            if (_isDirectPush)
                InitBroker();

            On(DisplayMessage, "Checking for unprocessed notifications...");
            PushNotification notificationEntity = pushNotification;

            try
            {
                if (notificationEntity != null)
                {
                    databaseContext.PushNotification.Add(pushNotification);
                    databaseContext.SaveChanges();

                    // reload the entity
                    notificationEntity = databaseContext.PushNotification
                        .Where(x => x.ID == pushNotification.ID)
                        .Include(x => x.MobileDevice)
                        .Include(x => x.MobileDevice.Client)
                        .FirstOrDefault();
                }
                else
                    notificationEntity = databaseContext.PushNotification.FirstOrDefault(s =>
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
                    UpdateNotificationQueued(notificationEntity);
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
                    UpdateNotificationQueued(notificationEntity);
                }
                //----------------------
                // WINDOWS NOTIFICATIONS
                //----------------------
                else if (notificationEntity.MobileDevice.SmartphonePlatform == "wp8")
                {
                    var wNotif = new WindowsToastNotification() { Tag = notificationEntity.ID };

                    wNotif.ForChannelUri(notificationEntity.MobileDevice.PushNotificationsRegistrationID)
                        .AsToastText01(notificationEntity.Message);

                    // WARNING: this would work for windows phone 8 silverlight, but not Windows Phone 8.1
                    //var wpNotif = new WindowsPhoneToastNotification() { Tag = notificationEntity.ID };
                    //wpNotif.ForEndpointUri(new Uri(Uri.UnescapeDataString(notificationEntity.MobileDevice.PushNotificationsRegistrationID)))
                    //    .ForOSVersion(WindowsPhoneDeviceOSVersion.Eight)
                    //    .WithBatchingInterval(BatchingInterval.Immediate)
                    //    .WithText1("PushSharpDemo")
                    //    .WithText2(notificationEntity.Message);

                    //var isValid = wpNotif.IsValidDeviceRegistrationId();

                    _broker.QueueNotification(wNotif);
                    UpdateNotificationQueued(notificationEntity);
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
                    // Save changes to DB to keep the correct state of messages
                    databaseContext.SaveChanges();
                    return true;
                }
                catch (Exception ex)
                {
                    On(DisplayErrorMessage, "EX. ERROR: Updating notification, DB save failed: " + ex.Message);
                    Error.Log(ex);
                    return false;
                }
            }
            else
            {
                if (_isDirectPush)
                    KillBroker(databaseContext);
                // no messages were queued, take a nap...
                return false;
            }
        }

        private static void UpdateNotificationQueued(PushNotification notificationEntity)
        {
            notificationEntity.Status = (int)PushNotificationStatus.Processing;
            notificationEntity.ModifiedAt = DateTime.Now;
            notificationEntity.Description = "(Processor) Notification queued for sending.";
        }

        private void DisposeContext(PushSharpDatabaseContext ctx, bool disposeContext)
        {
            if (disposeContext)
            {
                ctx.Dispose();
                On(DisplayMessage, "Database context sucessfully disposed.");
            }
        }

        private void broker_OnNotificationSent(object sender, INotification notification)
        {
            try
            {
                int ID = Convert.ToInt32(notification.Tag);

                var notif = _databaseContext.PushNotification.FirstOrDefault(s => s.ID == ID);
                if (notif != null)
                {
                    On(DisplayMessage, "ID " + notif.ID + " sent.");

                    notif.Description = "(Processor) Notification sent.";
                    notif.Status = (int)PushNotificationStatus.Processed;

                    _databaseContext.SaveChanges();
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
            finally
            {
                if (_isDirectPush)
                    KillBroker(_databaseContext);
            }
        }

        private void broker_OnDeviceSubscriptionExpired(object sender, string expiredSubscriptionId, DateTime expirationDateUtc, INotification notification)
        {
            On(DisplayErrorMessage, "Push Notification service subscription expired!");
        }

        private void broker_OnServiceException(object sender, Exception error)
        {
            On(DisplayErrorMessage, "Push Notification service failure!");
        }

        private void broker_OnNotificationFailed(object sender, INotification notification, Exception error)
        {
            On(DisplayErrorMessage, "Notification failed!");
        }

        private void broker_OnChannelException(object sender, IPushChannel pushChannel, Exception error)
        {
            On(DisplayErrorMessage, "Channel exception!");
        }

        private void broker_OnChannelDestroyed(object sender)
        {
            On(DisplayMessage, "Channel destroyed!");
        }

        private void broker_OnChannelCreated(object sender, IPushChannel pushChannel)
        {
            On(DisplayMessage, "Channel created!");
        }

        private void broker_OnDeviceSubscriptionChanged(object sender, string oldSubscriptionId, string newSubscriptionId, INotification notification)
        {
            On(DisplayMessage, "Device subscription changed!");
        }

        private void broker_OnNotificationRequeue(object sender, NotificationRequeueEventArgs e)
        {
            On(DisplayMessage, "Notification requed!");
        }

    }
}
