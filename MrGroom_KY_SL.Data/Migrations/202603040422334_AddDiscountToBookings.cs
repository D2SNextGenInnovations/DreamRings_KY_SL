namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDiscountToBookings : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Bookings", "DiscountValue", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.Bookings", "DiscountPercentage", c => c.Decimal(precision: 18, scale: 2));
            DropColumn("dbo.Payments", "DiscountValue");
            DropColumn("dbo.Payments", "DiscountPercentage");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Payments", "DiscountPercentage", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.Payments", "DiscountValue", c => c.Decimal(precision: 18, scale: 2));
            DropColumn("dbo.Bookings", "DiscountPercentage");
            DropColumn("dbo.Bookings", "DiscountValue");
        }
    }
}
