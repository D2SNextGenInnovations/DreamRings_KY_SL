using MrGroom_KY_SL.Common;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext() : base("AppDBConnection")
        {
            this.Configuration.LazyLoadingEnabled = false;// Ensure lazy loading is disabled
            this.Configuration.ProxyCreationEnabled = false;  // Disable proxy creation (no proxies)
        }

        static AppDbContext()
        {
            var ensure = System.Data.Entity.SqlServer.SqlProviderServices.Instance; Database.SetInitializer(new AppDbInitializer());
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<PackageItem> PackageItems { get; set; }
        public DbSet<PackagePhoto> PackagePhotos { get; set; }
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<PackageEventType> PackageEventTypes { get; set; }
        public DbSet<EmailHistory> EmailHistories { get; set; }
        public DbSet<CompanyInfo> CompanyInfos { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Booking <-> Staff many-to-many mapped to Booking_Staff
            modelBuilder.Entity<Booking>()
                .HasMany(b => b.StaffMembers)
                .WithMany(s => s.Bookings)
                .Map(m =>
                {
                    m.ToTable("Booking_Staff");
                    m.MapLeftKey("BookingId");
                    m.MapRightKey("StaffId");
                });

            modelBuilder.Entity<PackageItem>()
                .HasMany(p => p.PackageItemPackages)
                .WithRequired(pip => pip.PackageItem)
                .HasForeignKey(pip => pip.PackageItemId)
                .WillCascadeOnDelete(false);
        }
    }

    public class AppDbInitializer : CreateDatabaseIfNotExists<AppDbContext>
    {
        protected override void Seed(AppDbContext context)
        {
            var admin = new User { Username = "admin", Password = PasswordHelper.HashPassword("admin123"), Email = "admin@example.com", Role = "Admin" };
            context.Users.Add(admin);
            context.SaveChanges();
            base.Seed(context);
        }
    }
}
