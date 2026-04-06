using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MyCourierSA.Models;
using System;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyCourierSA.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Address { get; set; }
        public decimal WalletBalance { get; set; }= 50000m;
        public DateTime CreatedAt { get;set; }
        public bool IsActive { get; set; } = true;
        public DateTime? DeactivatedAt { get; set; }

        // Constructor to set default values
        public ApplicationUser()
        {
            WalletBalance = 50000m; // Every time a user object is created, it defaults to R50,000
        }



        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {

        public DbSet<ShipmentFile> ShipmentFiles { get; set; }
        public DbSet<StatusHistory> StatusHistories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<WarehouseBin> WarehouseBins { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<TicketReply> TicketReplies { get; set; }
        public DbSet<ShipmentRating> ShipmentRatings { get; set; }

        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {

        }


        public async Task<bool> AdjustWalletBalance(string userId, decimal amount, string description, string type)
        {
            // This executes a direct SQL command to ensure the math is done by the DB engine, 
            // preventing two users/requests from overwriting each other.
            var result = await Database.ExecuteSqlCommandAsync(
                "UPDATE AspNetUsers SET WalletBalance = WalletBalance + @p0 WHERE Id = @p1 AND (WalletBalance + @p0 >= 0)",
                amount, userId);

            if (result > 0)
            {
                this.WalletTransactions.Add(new WalletTransaction
                {
                    UserId = userId,
                    Amount = amount,
                    TransactionType = type,
                    Description = description,
                    Timestamp = DateTime.UtcNow
                });
                // Note: We don't call SaveChangesAsync here yet if we want it to be part of a larger transaction,
                // but for simple top-ups, we do.
                await this.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // This is always required!

            // FIX #1: For the ShipmentRating error
            modelBuilder.Entity<ShipmentRating>()
                .HasRequired(r => r.Shipment)
                .WithMany()
                .HasForeignKey(r => r.ShipmentId)
                .WillCascadeOnDelete(false);

            // FIX #2 (NEW): For the TicketReply error
            modelBuilder.Entity<TicketReply>()
                .HasRequired(r => r.SupportTicket)
                .WithMany(t => t.Replies) // The SupportTicket has a "Replies" collection
                .HasForeignKey(r => r.SupportTicketId)
                .WillCascadeOnDelete(false); // <-- THIS IS THE NEW FIX
        }
    }




}


