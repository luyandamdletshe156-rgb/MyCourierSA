using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace MyCourierSA.Models
{
    public class Shipment
    {
        public Shipment()
        {
            ShipmentFiles = new HashSet<ShipmentFile>();
            StatusHistories = new HashSet<StatusHistory>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public string CustomerId { get; set; } // FK to AspNetUsers

        [ForeignKey("CustomerId")]
        public virtual ApplicationUser Customer { get; set; }

        // ==========================================
        // SENDER INFO
        // ==========================================
        [Required, StringLength(100)]
        public string SenderName { get; set; }

        [StringLength(100)]
        public string SenderEmail { get; set; }

        [StringLength(20)]
        public string SenderPhone { get; set; }

        [Required, StringLength(250)]
        public string SenderAddress { get; set; }

        [Required, StringLength(100)] // Made REQUIRED for Route Planning
        public string SenderCity { get; set; }

        [Required, StringLength(100)] // Made REQUIRED
        public string SenderProvince { get; set; }
        public string ProofOfDeliveryPath { get; set; }
        // ==========================================
        // RECEIVER INFO
        // ==========================================
        [Required, StringLength(100)]
        public string ReceiverName { get; set; }

        [Required, StringLength(250)]
        public string ReceiverAddress { get; set; }

        [StringLength(100)]
        public string ReceiverEmail { get; set; }

        [StringLength(20)]
        public string ReceiverPhone { get; set; }

        [Required, StringLength(100)] // Made REQUIRED for Route Planning
        public string ReceiverCity { get; set; }

        [Required, StringLength(100)] // Made REQUIRED
        public string ReceiverProvince { get; set; }

        // ==========================================
        // PICKUP INFO
        // ==========================================
        [Required, StringLength(250)]
        public string PickupAddress { get; set; }
        public DateTime PickupDateTime { get; set; }

        // ==========================================
        // PARCEL INFO
        // ==========================================
        [Required, StringLength(50)]
        public string ParcelType { get; set; }
        public decimal ParcelWeight { get; set; }
        public bool IsFragile { get; set; }

        // ==========================================
        // DELIVERY & PRICING
        // ==========================================
        [StringLength(50)]
        public string DeliveryOption { get; set; }

        public decimal Price { get; set; }

        // ==========================================
        // SYSTEM & TRACKING
        // ==========================================
        [StringLength(50)]
        public string Status { get; set; } // e.g. Pending, Approved, At Warehouse, etc.

        [StringLength(50)]
        public string TrackingNumber { get; set; }

        public DateTime CreatedDate { get; set; }

        [DisplayName("Estimated Delivery")]
        public DateTime? EstimatedDeliveryDate { get; set; }

        [DisplayName("Actual Delivery")]
        public DateTime? ActualDeliveryDate { get; set; }

        // ==========================================
        // WAREHOUSE FIELDS 
        // ==========================================
        [StringLength(100)]
        public string Condition { get; set; } // e.g., "Good", "Damaged"

        [StringLength(100)]
        public string SortingBin { get; set; } // e.g., "LOCAL-FAST-TRACK"

        // ==========================================
        // RELATIONSHIPS
        // ==========================================
        public string AssignedDriverId { get; set; } // FK to ApplicationUser

        [ForeignKey("AssignedDriverId")]
        public virtual ApplicationUser AssignedDriver { get; set; }

        // Links to files and tracking timeline
        public virtual ICollection<ShipmentFile> ShipmentFiles { get; set; }
        public virtual ICollection<StatusHistory> StatusHistories { get; set; }
    }
}