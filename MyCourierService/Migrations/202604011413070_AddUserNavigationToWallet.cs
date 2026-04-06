namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserNavigationToWallet : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.WalletTransactions", "UserId", c => c.String(maxLength: 128));
            CreateIndex("dbo.WalletTransactions", "UserId");
            AddForeignKey("dbo.WalletTransactions", "UserId", "dbo.AspNetUsers", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.WalletTransactions", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.WalletTransactions", new[] { "UserId" });
            AlterColumn("dbo.WalletTransactions", "UserId", c => c.String());
        }
    }
}
