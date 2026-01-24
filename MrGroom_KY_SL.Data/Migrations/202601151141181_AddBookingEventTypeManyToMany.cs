namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBookingEventTypeManyToMany : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.Bookings", name: "EventTypeId", newName: "EventType_EventTypeId");
            RenameIndex(table: "dbo.Bookings", name: "IX_EventTypeId", newName: "IX_EventType_EventTypeId");
            CreateTable(
                "dbo.Booking_EventTypes",
                c => new
                    {
                        BookingEventTypeId = c.Int(nullable: false, identity: true),
                        BookingId = c.Int(nullable: false),
                        EventTypeId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.BookingEventTypeId)
                .ForeignKey("dbo.Bookings", t => t.BookingId, cascadeDelete: true)
                .ForeignKey("dbo.Event_Types", t => t.EventTypeId, cascadeDelete: true)
                .Index(t => t.BookingId)
                .Index(t => t.EventTypeId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Booking_EventTypes", "EventTypeId", "dbo.Event_Types");
            DropForeignKey("dbo.Booking_EventTypes", "BookingId", "dbo.Bookings");
            DropIndex("dbo.Booking_EventTypes", new[] { "EventTypeId" });
            DropIndex("dbo.Booking_EventTypes", new[] { "BookingId" });
            DropTable("dbo.Booking_EventTypes");
            RenameIndex(table: "dbo.Bookings", name: "IX_EventType_EventTypeId", newName: "IX_EventTypeId");
            RenameColumn(table: "dbo.Bookings", name: "EventType_EventTypeId", newName: "EventTypeId");
        }
    }
}
