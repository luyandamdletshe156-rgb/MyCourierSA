namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddWarehouseFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Shipments", "Condition", c => c.String());
            AddColumn("dbo.Shipments", "SortingBin", c => c.String());
            AlterColumn("dbo.Shipments", "CustomerId", c => c.String(nullable: false, maxLength: 128));
            CreateIndex("dbo.Shipments", "CustomerId");
            AddForeignKey("dbo.Shipments", "CustomerId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Shipments", "CustomerId", "dbo.AspNetUsers");
            DropIndex("dbo.Shipments", new[] { "CustomerId" });
            AlterColumn("dbo.Shipments", "CustomerId", c => c.String(nullable: false));
            DropColumn("dbo.Shipments", "SortingBin");
            DropColumn("dbo.Shipments", "Condition");
        }
    }
}
