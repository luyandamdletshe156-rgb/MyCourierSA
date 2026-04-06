using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCourierSA.Models
{
    public class TicketReply
    {
        public int Id { get; set; }

        [Required]
        public string Message { get; set; }

        public DateTime CreatedDate { get; set; }

        // --- Relationships ---

        // Who wrote this reply? (Can be a customer or an admin)
        [Required]
        public string AuthorId { get; set; }
        [ForeignKey("AuthorId")]
        public virtual ApplicationUser Author { get; set; }

        // Which ticket does this reply belong to?
        [Required]
        public int SupportTicketId { get; set; }
        [ForeignKey("SupportTicketId")]
        public virtual SupportTicket SupportTicket { get; set; }
    }
}