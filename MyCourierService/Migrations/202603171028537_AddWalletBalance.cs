namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddWalletBalance : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "WalletBalance", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "WalletBalance");
        }
    }
}
