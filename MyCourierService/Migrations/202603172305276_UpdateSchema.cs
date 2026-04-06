namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateSchema : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.InventoryItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ItemName = c.String(),
                        Quantity = c.Int(nullable: false),
                        Threshold = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.WarehouseBins",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        BinCode = c.String(),
                        IsOccupied = c.Boolean(nullable: false),
                        CurrentShipmentId = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.WarehouseBins");
            DropTable("dbo.InventoryItems");
        }
    }
}
