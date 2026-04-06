namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPODToShipment : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Shipments", "ProofOfDeliveryPath", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Shipments", "ProofOfDeliveryPath");
        }
    }
}
