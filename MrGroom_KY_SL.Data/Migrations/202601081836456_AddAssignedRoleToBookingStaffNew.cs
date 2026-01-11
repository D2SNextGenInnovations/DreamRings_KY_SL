namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAssignedRoleToBookingStaffNew : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.StaffBookings", "Staff_StaffId", "dbo.Staff");
            DropForeignKey("dbo.StaffBookings", "Booking_BookingId", "dbo.Bookings");
            DropForeignKey("dbo.Booking_Staff", "BookingId", "dbo.Bookings");
            DropForeignKey("dbo.Booking_Staff", "StaffId", "dbo.Staff");
            DropIndex("dbo.StaffBookings", new[] { "Staff_StaffId" });
            DropIndex("dbo.StaffBookings", new[] { "Booking_BookingId" });
            AddForeignKey("dbo.Booking_Staff", "BookingId", "dbo.Bookings", "BookingId");
            AddForeignKey("dbo.Booking_Staff", "StaffId", "dbo.Staff", "StaffId");
           
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.StaffBookings",
                c => new
                    {
                        Staff_StaffId = c.Int(nullable: false),
                        Booking_BookingId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Staff_StaffId, t.Booking_BookingId });
            
            DropForeignKey("dbo.Booking_Staff", "StaffId", "dbo.Staff");
            DropForeignKey("dbo.Booking_Staff", "BookingId", "dbo.Bookings");
            CreateIndex("dbo.StaffBookings", "Booking_BookingId");
            CreateIndex("dbo.StaffBookings", "Staff_StaffId");
            AddForeignKey("dbo.Booking_Staff", "StaffId", "dbo.Staff", "StaffId", cascadeDelete: true);
            AddForeignKey("dbo.Booking_Staff", "BookingId", "dbo.Bookings", "BookingId", cascadeDelete: true);
            AddForeignKey("dbo.StaffBookings", "Booking_BookingId", "dbo.Bookings", "BookingId", cascadeDelete: true);
            AddForeignKey("dbo.StaffBookings", "Staff_StaffId", "dbo.Staff", "StaffId", cascadeDelete: true);
        }
    }
}
