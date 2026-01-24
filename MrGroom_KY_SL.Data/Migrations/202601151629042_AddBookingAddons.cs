namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBookingAddons : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Booking_Addons",
                c => new
                    {
                        BookingAddonId = c.Int(nullable: false, identity: true),
                        BookingId = c.Int(nullable: false),
                        PackageItemId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.BookingAddonId)
                .ForeignKey("dbo.Bookings", t => t.BookingId, cascadeDelete: true)
                .ForeignKey("dbo.Package_Items", t => t.PackageItemId, cascadeDelete: true)
                .Index(t => t.BookingId)
                .Index(t => t.PackageItemId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Booking_Addons", "PackageItemId", "dbo.Package_Items");
            DropForeignKey("dbo.Booking_Addons", "BookingId", "dbo.Bookings");
            DropIndex("dbo.Booking_Addons", new[] { "PackageItemId" });
            DropIndex("dbo.Booking_Addons", new[] { "BookingId" });
            DropTable("dbo.Booking_Addons");
        }
    }
}
