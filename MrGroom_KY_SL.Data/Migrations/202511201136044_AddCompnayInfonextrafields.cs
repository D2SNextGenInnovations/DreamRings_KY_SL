namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCompnayInfonextrafields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CompanyInfo", "SecondaryPhone", c => c.String(maxLength: 50));
            AddColumn("dbo.CompanyInfo", "Web", c => c.String(maxLength: 100));
            AddColumn("dbo.CompanyInfo", "Fax", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CompanyInfo", "Fax");
            DropColumn("dbo.CompanyInfo", "Web");
            DropColumn("dbo.CompanyInfo", "SecondaryPhone");
        }
    }
}
