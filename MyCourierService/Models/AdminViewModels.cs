using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyCourierSA.Models
{
    public class DriverPayoutViewModel
    {
        public string DriverId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal TotalEarnedThisMonth { get; set; }
    }

    public class TransactionViewModel
    {
        public int Id { get; set; }
        public string UserEmail { get; set; }
        public string FullName { get; set; } // <--- ADD THIS
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }
}