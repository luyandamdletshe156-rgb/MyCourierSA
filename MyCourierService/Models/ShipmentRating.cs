using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCourierSA.Models
{
    public class ShipmentRating
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 5)]
        public int Stars { get; set; } // 1-5

        public string Comments { get; set; }

        public DateTime CreatedDate { get; set; }

        // --- Relationships ---

        // Which shipment is this rating for?
        [Required]
        public int ShipmentId { get; set; }
        [ForeignKey("ShipmentId")]
        public virtual Shipment Shipment { get; set; }

        // Who left the rating?
        [Required]
        public string CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual ApplicationUser Customer { get; set; }
    }
}