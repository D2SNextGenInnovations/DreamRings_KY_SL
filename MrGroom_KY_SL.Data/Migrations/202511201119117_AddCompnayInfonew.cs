namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCompnayInfonew : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CompanyInfo",
                c => new
                    {
                        CompanyInfoId = c.Int(nullable: false, identity: true),
                        CompanyName = c.String(nullable: false, maxLength: 100),
                        Address = c.String(maxLength: 250),
                        Phone = c.String(maxLength: 50),
                        Email = c.String(maxLength: 50),
                        CompanyLogo = c.Binary(),
                        CreatedOn = c.DateTime(nullable: false),
                        CreatedBy = c.String(),
                        ModifiedOn = c.DateTime(),
                        ModifiedBy = c.String(),
                    })
                .PrimaryKey(t => t.CompanyInfoId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.CompanyInfo");
        }
    }
}
