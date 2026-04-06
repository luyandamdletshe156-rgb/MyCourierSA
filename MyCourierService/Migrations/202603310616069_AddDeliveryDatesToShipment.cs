namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDeliveryDatesToShipment : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Shipments", "EstimatedDeliveryDate", c => c.DateTime());
            AddColumn("dbo.Shipments", "ActualDeliveryDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Shipments", "ActualDeliveryDate");
            DropColumn("dbo.Shipments", "EstimatedDeliveryDate");
        }
    }
}
