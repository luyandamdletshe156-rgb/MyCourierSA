namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSupportAndFeedbackSystem : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TicketReplies",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Message = c.String(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        AuthorId = c.String(nullable: false, maxLength: 128),
                        SupportTicketId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.AuthorId, cascadeDelete: true)
                .ForeignKey("dbo.SupportTickets", t => t.SupportTicketId)
                .Index(t => t.AuthorId)
                .Index(t => t.SupportTicketId);
            
            AddColumn("dbo.ShipmentRatings", "CreatedDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.ShipmentRatings", "CustomerId", c => c.String(nullable: false, maxLength: 128));
            AddColumn("dbo.SupportTickets", "InitialMessage", c => c.String(nullable: false));
            AddColumn("dbo.SupportTickets", "Priority", c => c.String(nullable: false, maxLength: 50));
            AddColumn("dbo.SupportTickets", "LastUpdatedDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.SupportTickets", "AssignedToStaffId", c => c.String(maxLength: 128));
            AddColumn("dbo.SupportTickets", "ShipmentId", c => c.Int());
            AlterColumn("dbo.SupportTickets", "CustomerId", c => c.String(nullable: false, maxLength: 128));
            AlterColumn("dbo.SupportTickets", "Subject", c => c.String(nullable: false, maxLength: 200));
            AlterColumn("dbo.SupportTickets", "Status", c => c.String(nullable: false, maxLength: 50));
            CreateIndex("dbo.ShipmentRatings", "ShipmentId");
            CreateIndex("dbo.ShipmentRatings", "CustomerId");
            CreateIndex("dbo.SupportTickets", "CustomerId");
            CreateIndex("dbo.SupportTickets", "AssignedToStaffId");
            CreateIndex("dbo.SupportTickets", "ShipmentId");
            AddForeignKey("dbo.ShipmentRatings", "CustomerId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ShipmentRatings", "ShipmentId", "dbo.Shipments", "Id");
            AddForeignKey("dbo.SupportTickets", "AssignedToStaffId", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.SupportTickets", "CustomerId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            AddForeignKey("dbo.SupportTickets", "ShipmentId", "dbo.Shipments", "Id");
            DropColumn("dbo.SupportTickets", "Message");
            DropTable("dbo.InventoryItems");
            DropTable("dbo.SupportClaims");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.SupportClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ShipmentId = c.Int(nullable: false),
                        Reason = c.String(),
                        Description = c.String(),
                        Status = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
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
            
            AddColumn("dbo.SupportTickets", "Message", c => c.String());
            DropForeignKey("dbo.TicketReplies", "SupportTicketId", "dbo.SupportTickets");
            DropForeignKey("dbo.TicketReplies", "AuthorId", "dbo.AspNetUsers");
            DropForeignKey("dbo.SupportTickets", "ShipmentId", "dbo.Shipments");
            DropForeignKey("dbo.SupportTickets", "CustomerId", "dbo.AspNetUsers");
            DropForeignKey("dbo.SupportTickets", "AssignedToStaffId", "dbo.AspNetUsers");
            DropForeignKey("dbo.ShipmentRatings", "ShipmentId", "dbo.Shipments");
            DropForeignKey("dbo.ShipmentRatings", "CustomerId", "dbo.AspNetUsers");
            DropIndex("dbo.TicketReplies", new[] { "SupportTicketId" });
            DropIndex("dbo.TicketReplies", new[] { "AuthorId" });
            DropIndex("dbo.SupportTickets", new[] { "ShipmentId" });
            DropIndex("dbo.SupportTickets", new[] { "AssignedToStaffId" });
            DropIndex("dbo.SupportTickets", new[] { "CustomerId" });
            DropIndex("dbo.ShipmentRatings", new[] { "CustomerId" });
            DropIndex("dbo.ShipmentRatings", new[] { "ShipmentId" });
            AlterColumn("dbo.SupportTickets", "Status", c => c.String());
            AlterColumn("dbo.SupportTickets", "Subject", c => c.String());
            AlterColumn("dbo.SupportTickets", "CustomerId", c => c.String());
            DropColumn("dbo.SupportTickets", "ShipmentId");
            DropColumn("dbo.SupportTickets", "AssignedToStaffId");
            DropColumn("dbo.SupportTickets", "LastUpdatedDate");
            DropColumn("dbo.SupportTickets", "Priority");
            DropColumn("dbo.SupportTickets", "InitialMessage");
            DropColumn("dbo.ShipmentRatings", "CustomerId");
            DropColumn("dbo.ShipmentRatings", "CreatedDate");
            DropTable("dbo.TicketReplies");
        }
    }
}
