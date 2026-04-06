namespace MyCourierSA.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddWalletTransactions : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.WalletTransactions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TransactionType = c.String(),
                        Description = c.String(),
                        Timestamp = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.WalletTransactions");
        }
    }
}
