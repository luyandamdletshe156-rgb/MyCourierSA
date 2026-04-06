using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyCourierSA.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        // This will hold the list of roles for the user
        public List<string> Roles { get; set; }
        public decimal WalletBalance { get; internal set; }
    }
}