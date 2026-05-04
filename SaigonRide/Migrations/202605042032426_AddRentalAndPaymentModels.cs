namespace SaigonRide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRentalAndPaymentModels : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Payments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        RentalId = c.Int(nullable: false),
                        Method = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PaidAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Rentals", t => t.RentalId)
                .Index(t => t.RentalId);
            
            CreateTable(
                "dbo.Rentals",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false),
                        VehicleId = c.Int(nullable: false),
                        StartStationId = c.Int(nullable: false),
                        ReturnStationId = c.Int(),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(),
                        BaseFare = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DiscountAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TotalFare = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Stations", t => t.ReturnStationId)
                .ForeignKey("dbo.Stations", t => t.StartStationId)
                .ForeignKey("dbo.Vehicles", t => t.VehicleId)
                .Index(t => t.VehicleId)
                .Index(t => t.StartStationId)
                .Index(t => t.ReturnStationId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Payments", "RentalId", "dbo.Rentals");
            DropForeignKey("dbo.Rentals", "VehicleId", "dbo.Vehicles");
            DropForeignKey("dbo.Rentals", "StartStationId", "dbo.Stations");
            DropForeignKey("dbo.Rentals", "ReturnStationId", "dbo.Stations");
            DropIndex("dbo.Rentals", new[] { "ReturnStationId" });
            DropIndex("dbo.Rentals", new[] { "StartStationId" });
            DropIndex("dbo.Rentals", new[] { "VehicleId" });
            DropIndex("dbo.Payments", new[] { "RentalId" });
            DropTable("dbo.Rentals");
            DropTable("dbo.Payments");
        }
    }
}
