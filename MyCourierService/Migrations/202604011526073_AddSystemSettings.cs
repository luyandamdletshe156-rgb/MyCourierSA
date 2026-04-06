namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSystemSettings : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.SystemSettings");
            AddColumn("dbo.SystemSettings", "Id", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.SystemSettings", "Description", c => c.String());
            AlterColumn("dbo.SystemSettings", "Key", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.SystemSettings", "Value", c => c.String(nullable: false));
            AddPrimaryKey("dbo.SystemSettings", "Id");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.SystemSettings");
            AlterColumn("dbo.SystemSettings", "Value", c => c.String());
            AlterColumn("dbo.SystemSettings", "Key", c => c.String(nullable: false, maxLength: 128));
            DropColumn("dbo.SystemSettings", "Description");
            DropColumn("dbo.SystemSettings", "Id");
            AddPrimaryKey("dbo.SystemSettings", "Key");
        }
    }
}
