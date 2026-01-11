namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPacakgeItemQty : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.PackageItemPackages", newName: "PackageItemPackage1");
            CreateTable(
                "dbo.PackageItemPackages",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PackageId = c.Int(nullable: false),
                        PackageItemId = c.Int(nullable: false),
                        Qty = c.Int(nullable: false),
                        CalculatedPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Packages", t => t.PackageId, cascadeDelete: true)
                .ForeignKey("dbo.Package_Items", t => t.PackageItemId, cascadeDelete: true)
                .Index(t => t.PackageId)
                .Index(t => t.PackageItemId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PackageItemPackages", "PackageItemId", "dbo.Package_Items");
            DropForeignKey("dbo.PackageItemPackages", "PackageId", "dbo.Packages");
            DropIndex("dbo.PackageItemPackages", new[] { "PackageItemId" });
            DropIndex("dbo.PackageItemPackages", new[] { "PackageId" });
            DropTable("dbo.PackageItemPackages");
            RenameTable(name: "dbo.PackageItemPackage1", newName: "PackageItemPackages");
        }
    }
}
