namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProvincesAndCities : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Shipments", "SenderCity", c => c.String());
            AddColumn("dbo.Shipments", "SenderProvince", c => c.String());
            AddColumn("dbo.Shipments", "ReceiverCity", c => c.String());
            AddColumn("dbo.Shipments", "ReceiverProvince", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Shipments", "ReceiverProvince");
            DropColumn("dbo.Shipments", "ReceiverCity");
            DropColumn("dbo.Shipments", "SenderProvince");
            DropColumn("dbo.Shipments", "SenderCity");
        }
    }
}
