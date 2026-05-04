namespace SaigonRide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SyncInventoryModels : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Stations",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        Capacity = c.Int(nullable: false),
                        CurrentInventory = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Vehicles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        VehicleCode = c.String(nullable: false),
                        VehicleCategoryId = c.Int(nullable: false),
                        StationId = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Stations", t => t.StationId, cascadeDelete: true)
                .ForeignKey("dbo.VehicleCategories", t => t.VehicleCategoryId, cascadeDelete: true)
                .Index(t => t.VehicleCategoryId)
                .Index(t => t.StationId);
            
            CreateTable(
                "dbo.VehicleCategories",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        PricePerMinute = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Vehicles", "VehicleCategoryId", "dbo.VehicleCategories");
            DropForeignKey("dbo.Vehicles", "StationId", "dbo.Stations");
            DropIndex("dbo.Vehicles", new[] { "StationId" });
            DropIndex("dbo.Vehicles", new[] { "VehicleCategoryId" });
            DropTable("dbo.VehicleCategories");
            DropTable("dbo.Vehicles");
            DropTable("dbo.Stations");
        }
    }
}
