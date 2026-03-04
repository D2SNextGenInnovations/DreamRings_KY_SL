namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCustomerWiseDiscount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Payments", "DiscountValue", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.Payments", "DiscountPercentage", c => c.Decimal(precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Payments", "DiscountPercentage");
            DropColumn("dbo.Payments", "DiscountValue");
        }
    }
}
