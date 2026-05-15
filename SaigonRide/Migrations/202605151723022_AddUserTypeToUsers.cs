namespace SaigonRide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserTypeToUsers : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "UserType", c => c.String(nullable: false, maxLength: 20, defaultValue: "Local"));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "UserType");
        }
    }
}
