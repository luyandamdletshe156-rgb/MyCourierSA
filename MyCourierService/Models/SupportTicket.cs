using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCourierSA.Models
{
    public class SupportTicket
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; }

        [Required]
        public string InitialMessage { get; set; } // Renamed from "Message"

        [Required]
        [StringLength(50)]
        public string Status { get; set; } // "Open", "In Progress", "Resolved"

        [Required]
        [StringLength(50)]
        public string Priority { get; set; } // "Low", "Normal", "High"

        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }

        // --- Relationships ---

        // Who created the ticket?
        [Required]
        public string CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual ApplicationUser Customer { get; set; }

        // Which Admin is assigned to handle it? (Optional)
        public string AssignedToStaffId { get; set; }
        [ForeignKey("AssignedToStaffId")]
        public virtual ApplicationUser AssignedToStaff { get; set; }

        // Is this ticket about a specific shipment? (Optional)
        public int? ShipmentId { get; set; }
        [ForeignKey("ShipmentId")]
        public virtual Shipment RelatedShipment { get; set; }

        // The list of all replies in this ticket's conversation
        public virtual ICollection<TicketReply> Replies { get; set; }
    }
}