namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPreviousStatusToBookings : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Bookings", "PreviousStatus", c => c.String(maxLength: 50));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Bookings", "PreviousStatus");
        }
    }
}
