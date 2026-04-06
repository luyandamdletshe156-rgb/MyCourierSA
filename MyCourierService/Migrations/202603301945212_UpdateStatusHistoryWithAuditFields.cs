namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateStatusHistoryWithAuditFields : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SystemSettings",
                c => new
                    {
                        Key = c.String(nullable: false, maxLength: 128),
                        Value = c.String(),
                    })
                .PrimaryKey(t => t.Key);
            
            AddColumn("dbo.StatusHistories", "Notes", c => c.String());
            AddColumn("dbo.StatusHistories", "Timestamp", c => c.DateTime(nullable: false));
            AddColumn("dbo.StatusHistories", "UpdatedById", c => c.String(maxLength: 128));
            AlterColumn("dbo.StatusHistories", "Status", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.StatusHistories", "Location", c => c.String(maxLength: 255));
            CreateIndex("dbo.StatusHistories", "UpdatedById");
            AddForeignKey("dbo.StatusHistories", "UpdatedById", "dbo.AspNetUsers", "Id");
            DropColumn("dbo.StatusHistories", "UpdatedAt");
        }
        
        public override void Down()
        {
            AddColumn("dbo.StatusHistories", "UpdatedAt", c => c.DateTime(nullable: false));
            DropForeignKey("dbo.StatusHistories", "UpdatedById", "dbo.AspNetUsers");
            DropIndex("dbo.StatusHistories", new[] { "UpdatedById" });
            AlterColumn("dbo.StatusHistories", "Location", c => c.String());
            AlterColumn("dbo.StatusHistories", "Status", c => c.String(nullable: false));
            DropColumn("dbo.StatusHistories", "UpdatedById");
            DropColumn("dbo.StatusHistories", "Timestamp");
            DropColumn("dbo.StatusHistories", "Notes");
            DropTable("dbo.SystemSettings");
        }
    }
}
