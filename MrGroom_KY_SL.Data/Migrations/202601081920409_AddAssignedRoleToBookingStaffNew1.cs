namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAssignedRoleToBookingStaffNew1 : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Booking_Staff", "BookingId", "dbo.Bookings");
            DropForeignKey("dbo.Booking_Staff", "StaffId", "dbo.Staff");
            DropPrimaryKey("dbo.Booking_Staff");
            AddPrimaryKey("dbo.Booking_Staff", new[] { "BookingId", "StaffId" });
            AddForeignKey("dbo.Booking_Staff", "BookingId", "dbo.Bookings", "BookingId", cascadeDelete: true);
            AddForeignKey("dbo.Booking_Staff", "StaffId", "dbo.Staff", "StaffId", cascadeDelete: true);
            Sql(@"
        IF EXISTS(SELECT * FROM sys.columns 
                  WHERE Name = N'BookingStaffId' AND Object_ID = Object_ID(N'dbo.Booking_Staff'))
            ALTER TABLE dbo.Booking_Staff DROP COLUMN BookingStaffId;

        IF EXISTS(SELECT * FROM sys.columns 
                  WHERE Name = N'AssignedRole' AND Object_ID = Object_ID(N'dbo.Booking_Staff'))
            ALTER TABLE dbo.Booking_Staff DROP COLUMN AssignedRole;
    ");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Booking_Staff", "AssignedRole", c => c.String(maxLength: 50));
            AddColumn("dbo.Booking_Staff", "BookingStaffId", c => c.Int(nullable: false, identity: true));
            DropForeignKey("dbo.Booking_Staff", "StaffId", "dbo.Staff");
            DropForeignKey("dbo.Booking_Staff", "BookingId", "dbo.Bookings");
            DropPrimaryKey("dbo.Booking_Staff");
            AddPrimaryKey("dbo.Booking_Staff", "BookingStaffId");
            AddForeignKey("dbo.Booking_Staff", "StaffId", "dbo.Staff", "StaffId");
            AddForeignKey("dbo.Booking_Staff", "BookingId", "dbo.Bookings", "BookingId");
        }
    }
}
