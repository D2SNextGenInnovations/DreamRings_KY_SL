namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addPacakeItemPackage : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.PackageItemPackages", "PackageItemId", "dbo.Package_Items");
            AddForeignKey("dbo.PackageItemPackages", "PackageItemId", "dbo.Package_Items", "PackageItemId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PackageItemPackages", "PackageItemId", "dbo.Package_Items");
            AddForeignKey("dbo.PackageItemPackages", "PackageItemId", "dbo.Package_Items", "PackageItemId", cascadeDelete: true);
        }
    }
}
