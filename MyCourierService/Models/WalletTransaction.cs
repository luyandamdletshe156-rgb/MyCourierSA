using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MyCourierSA.Models
{
    public class WalletTransaction
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
       
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } // "Deposit" or "Payment"
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
    }
}