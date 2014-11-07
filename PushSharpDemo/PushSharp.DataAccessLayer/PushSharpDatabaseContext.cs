namespace PushSharp.DataAccessLayer
{
    using System.Data.Entity;

    public partial class PushSharpDatabaseContext : DbContext
    {
        public PushSharpDatabaseContext()
            : base("name=DBModel")
        {
        }

        public virtual DbSet<Client> Client { get; set; }
        public virtual DbSet<MobileDevice> MobileDevice { get; set; }
        public virtual DbSet<PushNotification> PushNotification { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>()
                .HasMany(e => e.MobileDevice)
                .WithRequired(e => e.Client)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<MobileDevice>()
                .HasMany(e => e.PushNotification)
                .WithRequired(e => e.MobileDevice)
                .WillCascadeOnDelete(false);
        }
    }
}
