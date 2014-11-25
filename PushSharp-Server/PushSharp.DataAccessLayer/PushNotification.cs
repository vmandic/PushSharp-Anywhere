namespace PushSharp.DataAccessLayer
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("PushNotification")]
    public partial class PushNotification
    {
        public int ID { get; set; }

        public int MobileDeviceID { get; set; }

        public string Message { get; set; }

        public int Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ModifiedAt { get; set; }

        public string Description { get; set; }

        public virtual MobileDevice MobileDevice { get; set; }
    }
}
