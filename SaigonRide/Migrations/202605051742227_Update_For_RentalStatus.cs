namespace SaigonRide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Update_For_RentalStatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Rentals", "Status", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Rentals", "Status");
        }
    }
}
