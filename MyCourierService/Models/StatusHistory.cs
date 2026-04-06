using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCourierSA.Models
{
    public class StatusHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Shipment")]
        public int ShipmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        [StringLength(255)]
        public string Location { get; set; } // e.g., "Scanned at Johannesburg Warehouse"

        [Display(Name = "Additional Notes")]
        public string Notes { get; set; } // e.g., "Delivery attempted - recipient not available."

        [Required]
        public DateTime Timestamp { get; set; }

        [Display(Name = "Updated By")]
        public string UpdatedById { get; set; } // Foreign key to AspNetUsers

        // --- Navigation Properties ---

        [ForeignKey("ShipmentId")]
        public virtual Shipment Shipment { get; set; }

        [ForeignKey("UpdatedById")]
        public virtual ApplicationUser UpdatedBy { get; set; }
    }
}