namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddWarehouseRoutingFields : DbMigration
    {
        public override void Up()
        {
            Sql("UPDATE dbo.Shipments SET SenderCity = 'Unknown' WHERE SenderCity IS NULL");
            Sql("UPDATE dbo.Shipments SET SenderProvince = 'Unknown' WHERE SenderProvince IS NULL");
            Sql("UPDATE dbo.Shipments SET ReceiverCity = 'Unknown' WHERE ReceiverCity IS NULL");
            Sql("UPDATE dbo.Shipments SET ReceiverProvince = 'Unknown' WHERE ReceiverProvince IS NULL");

            AlterColumn("dbo.Shipments", "SenderName", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Shipments", "SenderEmail", c => c.String(maxLength: 100));
            AlterColumn("dbo.Shipments", "SenderPhone", c => c.String(maxLength: 20));
            AlterColumn("dbo.Shipments", "SenderAddress", c => c.String(nullable: false, maxLength: 250));
            AlterColumn("dbo.Shipments", "SenderCity", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Shipments", "SenderProvince", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Shipments", "ReceiverName", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Shipments", "ReceiverAddress", c => c.String(nullable: false, maxLength: 250));
            AlterColumn("dbo.Shipments", "ReceiverEmail", c => c.String(maxLength: 100));
            AlterColumn("dbo.Shipments", "ReceiverPhone", c => c.String(maxLength: 20));
            AlterColumn("dbo.Shipments", "ReceiverCity", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Shipments", "ReceiverProvince", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Shipments", "PickupAddress", c => c.String(nullable: false, maxLength: 250));
            AlterColumn("dbo.Shipments", "ParcelType", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.Shipments", "DeliveryOption", c => c.String(maxLength: 50));
            AlterColumn("dbo.Shipments", "Status", c => c.String(maxLength: 50));
            AlterColumn("dbo.Shipments", "TrackingNumber", c => c.String(maxLength: 50));
            AlterColumn("dbo.Shipments", "Condition", c => c.String(maxLength: 100));
            AlterColumn("dbo.Shipments", "SortingBin", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Shipments", "SortingBin", c => c.String());
            AlterColumn("dbo.Shipments", "Condition", c => c.String());
            AlterColumn("dbo.Shipments", "TrackingNumber", c => c.String());
            AlterColumn("dbo.Shipments", "Status", c => c.String());
            AlterColumn("dbo.Shipments", "DeliveryOption", c => c.String());
            AlterColumn("dbo.Shipments", "ParcelType", c => c.String(nullable: false));
            AlterColumn("dbo.Shipments", "PickupAddress", c => c.String(nullable: false));
            AlterColumn("dbo.Shipments", "ReceiverProvince", c => c.String());
            AlterColumn("dbo.Shipments", "ReceiverCity", c => c.String());
            AlterColumn("dbo.Shipments", "ReceiverPhone", c => c.String());
            AlterColumn("dbo.Shipments", "ReceiverEmail", c => c.String());
            AlterColumn("dbo.Shipments", "ReceiverAddress", c => c.String(nullable: false));
            AlterColumn("dbo.Shipments", "ReceiverName", c => c.String(nullable: false));
            AlterColumn("dbo.Shipments", "SenderProvince", c => c.String());
            AlterColumn("dbo.Shipments", "SenderCity", c => c.String());
            AlterColumn("dbo.Shipments", "SenderAddress", c => c.String());
            AlterColumn("dbo.Shipments", "SenderPhone", c => c.String());
            AlterColumn("dbo.Shipments", "SenderEmail", c => c.String());
            AlterColumn("dbo.Shipments", "SenderName", c => c.String());
        }
    }
}
