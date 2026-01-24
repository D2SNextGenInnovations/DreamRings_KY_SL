namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MakeEventTypeOptionalInBooking : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Bookings", "EventTypeId", "dbo.Event_Types");
            DropIndex("dbo.Bookings", new[] { "EventTypeId" });
            AlterColumn("dbo.Bookings", "EventTypeId", c => c.Int());
            CreateIndex("dbo.Bookings", "EventTypeId");
            AddForeignKey("dbo.Bookings", "EventTypeId", "dbo.Event_Types", "EventTypeId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Bookings", "EventTypeId", "dbo.Event_Types");
            DropIndex("dbo.Bookings", new[] { "EventTypeId" });
            AlterColumn("dbo.Bookings", "EventTypeId", c => c.Int(nullable: false));
            CreateIndex("dbo.Bookings", "EventTypeId");
            AddForeignKey("dbo.Bookings", "EventTypeId", "dbo.Event_Types", "EventTypeId", cascadeDelete: true);
        }
    }
}
