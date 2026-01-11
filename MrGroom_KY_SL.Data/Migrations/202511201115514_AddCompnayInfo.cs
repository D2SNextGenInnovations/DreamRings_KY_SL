namespace MrGroom_KY_SL.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCompnayInfo : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Bookings",
                c => new
                    {
                        BookingId = c.Int(nullable: false, identity: true),
                        CustomerId = c.Int(nullable: false),
                        EventTypeId = c.Int(nullable: false),
                        PackageId = c.Int(nullable: false),
                        Location = c.String(nullable: false),
                        EventDate = c.DateTime(nullable: false),
                        BookingDate = c.DateTime(nullable: false),
                        Status = c.String(maxLength: 50),
                        Notes = c.String(),
                    })
                .PrimaryKey(t => t.BookingId)
                .ForeignKey("dbo.Customers", t => t.CustomerId, cascadeDelete: true)
                .ForeignKey("dbo.Event_Types", t => t.EventTypeId, cascadeDelete: true)
                .ForeignKey("dbo.Packages", t => t.PackageId, cascadeDelete: true)
                .Index(t => t.CustomerId)
                .Index(t => t.EventTypeId)
                .Index(t => t.PackageId);
            
            CreateTable(
                "dbo.Customers",
                c => new
                    {
                        CustomerId = c.Int(nullable: false, identity: true),
                        FirstName = c.String(nullable: false, maxLength: 200),
                        LastName = c.String(maxLength: 200),
                        Email = c.String(maxLength: 200),
                        Phone = c.String(maxLength: 50),
                        NICNumber = c.String(maxLength: 50),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.CustomerId);
            
            CreateTable(
                "dbo.CustomerAddress",
                c => new
                    {
                        AddressId = c.Int(nullable: false, identity: true),
                        CustomerId = c.Int(nullable: false),
                        AddressLine1 = c.String(nullable: false, maxLength: 200),
                        AddressLine2 = c.String(maxLength: 200),
                        AddressLine3 = c.String(maxLength: 200),
                        City = c.String(nullable: false, maxLength: 100),
                        StateOrProvince = c.String(maxLength: 100),
                        PostalCode = c.String(maxLength: 20),
                        Country = c.String(nullable: false, maxLength: 100),
                        AddressType = c.String(maxLength: 50),
                        IsPrimary = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        UpdatedDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.AddressId)
                .ForeignKey("dbo.Customers", t => t.CustomerId, cascadeDelete: true)
                .Index(t => t.CustomerId);
            
            CreateTable(
                "dbo.Event_Types",
                c => new
                    {
                        EventTypeId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsActive = c.Boolean(),
                    })
                .PrimaryKey(t => t.EventTypeId);
            
            CreateTable(
                "dbo.Package_Event_Types",
                c => new
                    {
                        PackageEventTypeId = c.Int(nullable: false, identity: true),
                        PackageId = c.Int(nullable: false),
                        EventTypeId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PackageEventTypeId)
                .ForeignKey("dbo.Event_Types", t => t.EventTypeId, cascadeDelete: true)
                .ForeignKey("dbo.Packages", t => t.PackageId, cascadeDelete: true)
                .Index(t => t.PackageId)
                .Index(t => t.EventTypeId);
            
            CreateTable(
                "dbo.Packages",
                c => new
                    {
                        PackageId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 150),
                        Description = c.String(),
                        BasePrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DurationHours = c.Int(),
                        IsActive = c.Boolean(nullable: false),
                        IsIncludeSodftCopies = c.Boolean(nullable: false),
                        EditedPhotosCount = c.Int(),
                    })
                .PrimaryKey(t => t.PackageId);
            
            CreateTable(
                "dbo.Package_Items",
                c => new
                    {
                        PackageItemId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 150),
                        Description = c.String(),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsActive = c.Boolean(),
                    })
                .PrimaryKey(t => t.PackageItemId);
            
            CreateTable(
                "dbo.Package_Photos",
                c => new
                    {
                        PhotoId = c.Int(nullable: false, identity: true),
                        PackageId = c.Int(nullable: false),
                        PhotoUrl = c.String(nullable: false, maxLength: 255),
                        DisplayOrder = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PhotoId)
                .ForeignKey("dbo.Packages", t => t.PackageId, cascadeDelete: true)
                .Index(t => t.PackageId);
            
            CreateTable(
                "dbo.Payments",
                c => new
                    {
                        PaymentId = c.Int(nullable: false, identity: true),
                        BookingId = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PaymentDate = c.DateTime(nullable: false),
                        PaymentMethod = c.String(maxLength: 50),
                        PaymentType = c.String(maxLength: 50),
                        Status = c.String(maxLength: 50),
                        Remarks = c.String(maxLength: 250),
                    })
                .PrimaryKey(t => t.PaymentId)
                .ForeignKey("dbo.Bookings", t => t.BookingId, cascadeDelete: true)
                .Index(t => t.BookingId);
            
            CreateTable(
                "dbo.Staff",
                c => new
                    {
                        StaffId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 150),
                        Role = c.String(maxLength: 50),
                        Phone = c.String(maxLength: 20),
                        Email = c.String(maxLength: 100),
                        IsActive = c.Boolean(),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.StaffId);
            
            CreateTable(
                "dbo.EmailHistory",
                c => new
                    {
                        EmailHistoryId = c.Int(nullable: false, identity: true),
                        BookingId = c.Int(),
                        RecipientEmail = c.String(nullable: false, maxLength: 200),
                        Subject = c.String(nullable: false, maxLength: 300),
                        MessageBody = c.String(),
                        SentAt = c.DateTime(nullable: false),
                        Status = c.String(maxLength: 50),
                        ErrorMessage = c.String(),
                    })
                .PrimaryKey(t => t.EmailHistoryId)
                .ForeignKey("dbo.Bookings", t => t.BookingId)
                .Index(t => t.BookingId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        FirstName = c.String(maxLength: 50),
                        LastName = c.String(maxLength: 75),
                        Gender = c.String(maxLength: 20),
                        Username = c.String(nullable: false, maxLength: 50),
                        Password = c.String(nullable: false, maxLength: 200),
                        Email = c.String(nullable: false, maxLength: 200),
                        Role = c.String(nullable: false, maxLength: 20),
                        Photo = c.Binary(),
                        IsActive = c.Boolean(),
                        CreatedOn = c.DateTime(),
                        CreatedBy = c.String(),
                        ModifiedOn = c.DateTime(),
                        ModifiedBy = c.String(),
                    })
                .PrimaryKey(t => t.UserId);
            
            CreateTable(
                "dbo.PackageItemPackages",
                c => new
                    {
                        PackageItem_PackageItemId = c.Int(nullable: false),
                        Package_PackageId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.PackageItem_PackageItemId, t.Package_PackageId })
                .ForeignKey("dbo.Package_Items", t => t.PackageItem_PackageItemId, cascadeDelete: true)
                .ForeignKey("dbo.Packages", t => t.Package_PackageId, cascadeDelete: true)
                .Index(t => t.PackageItem_PackageItemId)
                .Index(t => t.Package_PackageId);
            
            CreateTable(
                "dbo.Booking_Staff",
                c => new
                    {
                        BookingId = c.Int(nullable: false),
                        StaffId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.BookingId, t.StaffId })
                .ForeignKey("dbo.Bookings", t => t.BookingId, cascadeDelete: true)
                .ForeignKey("dbo.Staff", t => t.StaffId, cascadeDelete: true)
                .Index(t => t.BookingId)
                .Index(t => t.StaffId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.EmailHistory", "BookingId", "dbo.Bookings");
            DropForeignKey("dbo.Booking_Staff", "StaffId", "dbo.Staff");
            DropForeignKey("dbo.Booking_Staff", "BookingId", "dbo.Bookings");
            DropForeignKey("dbo.Payments", "BookingId", "dbo.Bookings");
            DropForeignKey("dbo.Bookings", "PackageId", "dbo.Packages");
            DropForeignKey("dbo.Bookings", "EventTypeId", "dbo.Event_Types");
            DropForeignKey("dbo.Package_Event_Types", "PackageId", "dbo.Packages");
            DropForeignKey("dbo.Package_Photos", "PackageId", "dbo.Packages");
            DropForeignKey("dbo.PackageItemPackages", "Package_PackageId", "dbo.Packages");
            DropForeignKey("dbo.PackageItemPackages", "PackageItem_PackageItemId", "dbo.Package_Items");
            DropForeignKey("dbo.Package_Event_Types", "EventTypeId", "dbo.Event_Types");
            DropForeignKey("dbo.Bookings", "CustomerId", "dbo.Customers");
            DropForeignKey("dbo.CustomerAddress", "CustomerId", "dbo.Customers");
            DropIndex("dbo.Booking_Staff", new[] { "StaffId" });
            DropIndex("dbo.Booking_Staff", new[] { "BookingId" });
            DropIndex("dbo.PackageItemPackages", new[] { "Package_PackageId" });
            DropIndex("dbo.PackageItemPackages", new[] { "PackageItem_PackageItemId" });
            DropIndex("dbo.EmailHistory", new[] { "BookingId" });
            DropIndex("dbo.Payments", new[] { "BookingId" });
            DropIndex("dbo.Package_Photos", new[] { "PackageId" });
            DropIndex("dbo.Package_Event_Types", new[] { "EventTypeId" });
            DropIndex("dbo.Package_Event_Types", new[] { "PackageId" });
            DropIndex("dbo.CustomerAddress", new[] { "CustomerId" });
            DropIndex("dbo.Bookings", new[] { "PackageId" });
            DropIndex("dbo.Bookings", new[] { "EventTypeId" });
            DropIndex("dbo.Bookings", new[] { "CustomerId" });
            DropTable("dbo.Booking_Staff");
            DropTable("dbo.PackageItemPackages");
            DropTable("dbo.Users");
            DropTable("dbo.EmailHistory");
            DropTable("dbo.Staff");
            DropTable("dbo.Payments");
            DropTable("dbo.Package_Photos");
            DropTable("dbo.Package_Items");
            DropTable("dbo.Packages");
            DropTable("dbo.Package_Event_Types");
            DropTable("dbo.Event_Types");
            DropTable("dbo.CustomerAddress");
            DropTable("dbo.Customers");
            DropTable("dbo.Bookings");
        }
    }
}
