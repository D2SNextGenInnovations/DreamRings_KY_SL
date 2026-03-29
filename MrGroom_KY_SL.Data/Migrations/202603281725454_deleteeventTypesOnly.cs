namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class deleteeventTypesOnly : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Package_Event_Types", "PackageId", "dbo.Packages");
            DropForeignKey("dbo.Package_Event_Types", "EventTypeId", "dbo.Event_Types");
            AddColumn("dbo.Package_Event_Types", "Package_PackageId", c => c.Int());
            CreateIndex("dbo.Package_Event_Types", "Package_PackageId");
            AddForeignKey("dbo.Package_Event_Types", "Package_PackageId", "dbo.Packages", "PackageId");
            AddForeignKey("dbo.Package_Event_Types", "EventTypeId", "dbo.Event_Types", "EventTypeId");
            AddForeignKey("dbo.Package_Event_Types", "PackageId", "dbo.Packages", "PackageId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Package_Event_Types", "PackageId", "dbo.Packages");
            DropForeignKey("dbo.Package_Event_Types", "EventTypeId", "dbo.Event_Types");
            DropForeignKey("dbo.Package_Event_Types", "Package_PackageId", "dbo.Packages");
            DropIndex("dbo.Package_Event_Types", new[] { "Package_PackageId" });
            DropColumn("dbo.Package_Event_Types", "Package_PackageId");
            AddForeignKey("dbo.Package_Event_Types", "EventTypeId", "dbo.Event_Types", "EventTypeId", cascadeDelete: true);
            AddForeignKey("dbo.Package_Event_Types", "PackageId", "dbo.Packages", "PackageId", cascadeDelete: true);
        }
    }
}
