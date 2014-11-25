using Newtonsoft.Json;
using PushSharp.Android;
using PushSharp.Apple;
using PushSharp.Core;
using PushSharp.CoreProcessor.Utility;
using PushSharp.DataAccessLayer;
using PushSharp.Windows;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;

namespace PushSharp.CoreProcessor
{
    public class PushNotificationProcessor
    {
        // Thread Locker
        private readonly object _lock = new object();

        // Fields
        private PushSharpDatabaseContext _databaseContext;
        private Thread _processorThread;
        private bool _processorThreadRunning = false;
        private bool _isDirectSinglePush = false;
        private PushBroker _broker;
        private readonly string _googlePushNotificationAuthToken;
        private readonly byte[] _applePushNotificationCertificate;
        private readonly WindowsPushChannelSettings _windowsPushNotificationChannelSettings;

        // The thread safe lazy accessor for the database context
        protected PushSharpDatabaseContext DatabaseContext
        {
            get
            {
                lock (_lock)
                {
                    if (this._databaseContext == null)
                        this._databaseContext = new PushSharpDatabaseContext();

                    return this._databaseContext;
                }
            }
        }

        // Consts
        public readonly int THREAD_SLEEP_DURATION_MILISECONDS = 1000;

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
            // REQUIRED PLATFORM AUTHORIZATION TOKENS, a specific procedure for each platform, yeah, boring, I know...
            // register for those at: https://developer.apple.com/ and https://developers.google.com/ and https://dev.windows.com/en-us

            _googlePushNotificationAuthToken = "ENTER SERVER KEY";
            _applePushNotificationCertificate = new byte[] { };//File.ReadAllBytes("/Certificates/Apple-PushNotifications-DevCert.p12");
            _windowsPushNotificationChannelSettings = new WindowsPushChannelSettings
            (
                "DevUG PushSharp",
                "ENTER CHANNEL KEY",
                "ENTER SECRET"
            );
        }

        /// <summary>
        /// Initilizes the push broker instance and creates a new DbContext, checks for both if there is no instance, if so creates a new one.
        /// </summary>
        private void InitBroker()
        {
            _broker = _broker ?? new PushBroker();
            _databaseContext = _databaseContext ?? new PushSharpDatabaseContext();

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
            _broker.RegisterWindowsService(_windowsPushNotificationChannelSettings);

            On(DisplayMessage, "Push Broker successfully initialized.");
        }

        /// <summary>
        /// Kills the push broker instance by frist stoping it's services and unhooking the event handlers, then disposes the sent DbContext.
        /// </summary>
        /// <param name="dbContext">The current EF database context instance to be disposed.</param>
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

        /// <summary>
        /// Inits the push broker and starts the processor on a separate thread.
        /// </summary>
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

        /// <summary>
        /// Stops the processor thread and then kills the push broker.
        /// </summary>
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

        /// <summary>
        /// The main action to be run on a separate new thread which processes all incoming notifications on/from the database.
        /// </summary>
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
                    Thread.Sleep(THREAD_SLEEP_DURATION_MILISECONDS);
                }
            }

            On(DisplayMessage, "Notification processor thread stopped.");
            On(DisplayStatusMessage, "Idle.");
        }

        /// <summary>
        /// A processor method used to process multiple notifications at once.
        /// </summary>
        /// <param name="databaseContext">The current database context to be used for processing to the database.</param>
        /// <param name="pushNotifications">A list of push notifitcation entities to be processed and saved.</param>
        /// <returns>True if all OK, false if sth. goes wrong.</returns>
        public bool ProcessNotifications(PushSharpDatabaseContext databaseContext, IList<PushNotification> pushNotifications)
        {
            _databaseContext = databaseContext;

            try
            {
                // runs the broker during the loop, then stops
                InitBroker();

                foreach (var pushNotification in pushNotifications)
                {
                    if (!ProcessNotification(_databaseContext, pushNotification, false))
                        throw new Exception("INTERNAL EX. ERROR: Error on processing a single notification in ProcessNotifications method!");
                }

                // stop the broker, all should be processed...
                KillBroker(_databaseContext);
                return true;
            }
            catch (Exception ex)
            {
                On(DisplayErrorMessage, "EX. ERROR: Saving and pushing multiple notifications: " + ex.Message);
                SimpleErrorLogger.Log(ex);
                return false;
            }
        }

        /// <summary>
        /// The main processor method used to process a single push notification, checks if the processing will be an immediate single push or regular thread looping model. 
        /// Looks up for a single entity in the databae which has not been processed. 
        /// Puts the fetched unprocessed push notification entity to processing over the Push Sharp API. 
        /// Finally saves the state of processing.
        /// </summary>
        /// <param name="databaseContext">The database context used for fetching and persisting data.</param>
        /// <param name="pushNotification">A single push notification entity to be processed and saved.</param>
        /// <param name="isDirectSinglePush">Decides wethere the processing will take place immediately for the sent notification or will the method lookup from the database for a first unprocessed push notification.</param>
        /// <returns>True if all OK, false if not.</returns>
        public bool ProcessNotification(PushSharpDatabaseContext databaseContext, PushNotification pushNotification = null, bool isDirectSinglePush = false)
        {
            _isDirectSinglePush = isDirectSinglePush;
            _databaseContext = databaseContext;

            if (_isDirectSinglePush)
                InitBroker();

            On(DisplayMessage, "Checking for unprocessed notifications...");
            PushNotification notificationEntity = pushNotification;

            try
            {
                if (notificationEntity != null)
                {
                    // save a new immediate unprocessed push notification
                    _databaseContext.PushNotification.Add(pushNotification);
                    _databaseContext.SaveChanges();

                    // reload the entity
                    notificationEntity = _databaseContext.PushNotification
                        .Where(x => x.ID == pushNotification.ID)
                        .Include(x => x.MobileDevice)
                        .Include(x => x.MobileDevice.Client)
                        .FirstOrDefault();
                }
                else
                    notificationEntity = _databaseContext.PushNotification.FirstOrDefault(s =>
                                    s.Status == (int)PushNotificationStatus.Unprocessed &&
                                    s.CreatedAt <= DateTime.Now);
            }
            catch (Exception ex)
            {
                On(DisplayErrorMessage, "EX. ERROR: Check for unprocessed notifications: " + ex.Message);
                SimpleErrorLogger.Log(ex);
            }

            // Process i.e. push the push notification via PushSharp... 
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
                else if (notificationEntity.MobileDevice.SmartphonePlatform.Equals("wp") || notificationEntity.MobileDevice.SmartphonePlatform.Equals("wsa"))
                {
                    var wNotif = new WindowsToastNotification() { Tag = notificationEntity.ID };

                    wNotif.ForChannelUri(notificationEntity.MobileDevice.PushNotificationsRegistrationID)
                        .AsToastText01(notificationEntity.Message);

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
                    _databaseContext.SaveChanges();
                    return true;
                }
                catch (Exception ex)
                {
                    On(DisplayErrorMessage, "EX. ERROR: Updating notification, DB save failed: " + ex.Message);
                    SimpleErrorLogger.Log(ex);
                    return false;
                }
            }
            else
            {
                if (_isDirectSinglePush)
                    KillBroker(_databaseContext);

                // no messages were queued, take a nap...
                return false;
            }
        }

        /// <summary>
        /// Enqueues a single notification on the database.
        /// </summary>
        /// <param name="databaseContext">The database context used for saving.</param>
        /// <param name="pushNotification">A push notification to be saved for later processing.</param>
        /// <param name="saveAndDisposeContext"></param>
        /// <returns>True if all OK, false if not.</returns>
        public bool EnqueueNotificationOnDatabase(PushSharpDatabaseContext databaseContext, PushNotification pushNotification, bool saveAndDisposeContext = true)
        {
            try
            {
                databaseContext.PushNotification.Add(pushNotification);

                if (saveAndDisposeContext)
                {
                    databaseContext.SaveChanges();
                    databaseContext.Dispose();
                }

                return true;
            }
            catch (Exception ex)
            {
                On(DisplayErrorMessage, "EX. ERROR: Enqueuing notification, DB save failed: " + ex.Message);
                SimpleErrorLogger.Log(ex);
                return false;
            }
        }

        /// <summary>
        /// Enqueues a list of push notifications on the database to be pushed when the processor runs.
        /// </summary>
        /// <param name="databaseContext">The database context used for saving.</param>
        /// <param name="pushNotifications">The list of push notifications on the database to be pushed when the processor runs</param>
        /// <returns>True if all OK, false if not.</returns>
        public bool EnqueueNotificationsOnDatabase(PushSharpDatabaseContext databaseContext, List<PushNotification> pushNotifications)
        {
            try
            {
                foreach (var pushNotification in pushNotifications)
                    if (!EnqueueNotificationOnDatabase(databaseContext, pushNotification, false))
                        throw new Exception("INTERNAL EX. ERROR: Error on adding a single notification in EnqueueNotificationsOnDatabase method!");

                databaseContext.SaveChanges();
                databaseContext.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                On(DisplayErrorMessage, "EX. ERROR: Enqueuing notification, DB save failed: " + ex.Message);
                SimpleErrorLogger.Log(ex);
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
                ctx = null;
                _databaseContext = null;
                On(DisplayMessage, "Database context sucessfully disposed.");
            }
        }

        private void broker_OnNotificationSent(object sender, INotification notification)
        {
            try
            {
                int ID = Convert.ToInt32(notification.Tag);

                var notificationEntity = _databaseContext.PushNotification.FirstOrDefault(s => s.ID == ID);
                if (notificationEntity != null)
                {
                    On(DisplayMessage, "ID " + notificationEntity.ID + " sent.");

                    notificationEntity.Description = "(Processor) Notification sent.";
                    notificationEntity.Status = (int)PushNotificationStatus.Processed;

                    _databaseContext.SaveChanges();
                }
                else
                {
                    On(DisplayErrorMessage, "ERROR: Notification " + notificationEntity.ID + " not found in database!");
                }
            }
            catch (Exception ex)
            {
                On(DisplayErrorMessage, "EX. ERROR: Get notification from DB: " + ex.Message);
                SimpleErrorLogger.Log(ex);
            }
            finally
            {
                if (_isDirectSinglePush)
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
            SimpleErrorLogger.Log(error);
        }

        /// <summary>
        /// This one will fail if your APNS, GCM and WIN registration data is not correct, also if a notification is malformed.
        /// </summary>
        /// <remarks>If this one fails check out the inner exception and the base object data for details!</remarks>
        private void broker_OnNotificationFailed(object sender, INotification notification, Exception error)
        {
            try
            {
                int ID = Convert.ToInt32(notification.Tag);

                var notificationEntity = _databaseContext.PushNotification.FirstOrDefault(s => s.ID == ID);
                if (notificationEntity != null)
                {
                    On(DisplayErrorMessage, "ID " + notificationEntity.ID + " failed.");

                    notificationEntity.Description = "(Processor) Notification failed.";
                    notificationEntity.Status = (int)PushNotificationStatus.Error;

                    _databaseContext.SaveChanges();
                }
                else
                {
                    On(DisplayErrorMessage, "ERROR: The failed notification " + notificationEntity.ID + " not found in database!");
                }
            }
            catch (Exception ex)
            {
                On(DisplayErrorMessage, "Notification failed handler failed hard! :-)");
                SimpleErrorLogger.Log(ex);
            }

            On(DisplayErrorMessage, "Notification failed!");
            SimpleErrorLogger.Log(error);
        }

        private void broker_OnChannelException(object sender, IPushChannel pushChannel, Exception error)
        {
            On(DisplayErrorMessage, "Channel exception!");
            SimpleErrorLogger.Log(error);
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
            On(DisplayMessage, "Notification requeued!");
        }
    }
}
