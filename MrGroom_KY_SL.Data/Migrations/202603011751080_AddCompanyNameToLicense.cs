namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCompanyNameToLicense : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Licenses",
                c => new
                    {
                        LicenseId = c.Int(nullable: false, identity: true),
                        ProductKey = c.String(nullable: false),
                        ActivatedOn = c.DateTime(nullable: false),
                        ExpiryDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.LicenseId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Licenses");
        }
    }
}
