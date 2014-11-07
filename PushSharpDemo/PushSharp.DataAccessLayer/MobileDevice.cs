namespace PushSharp.DataAccessLayer
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MobileDevice")]
    public partial class MobileDevice
    {
        public MobileDevice()
        {
            PushNotification = new HashSet<PushNotification>();
        }

        public int ID { get; set; }

        public int ClientID { get; set; }

        public string SmartphonePlatform { get; set; }

        public string PushNotificationsRegistrationID { get; set; }

        public bool Active { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string DeviceID { get; set; }

        public virtual Client Client { get; set; }

        public virtual ICollection<PushNotification> PushNotification { get; set; }
    }
}
