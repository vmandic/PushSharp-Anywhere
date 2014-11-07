namespace PushSharp.DataAccessLayer
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Client")]
    public partial class Client
    {
        public Client()
        {
            MobileDevice = new HashSet<MobileDevice>();
        }

        public int ID { get; set; }

        [Required]
        public string Username { get; set; }

        public virtual ICollection<MobileDevice> MobileDevice { get; set; }
    }
}
