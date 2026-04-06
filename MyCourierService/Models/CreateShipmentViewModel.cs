using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace MyCourierSA.Models
{
    public class CreateShipmentViewModel
    {
        // =========================
        // Sender Information
        // =========================

        [Required]
        [Display(Name = "Sender Name")]
        public string SenderName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Sender Email")]
        public string SenderEmail { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Sender Phone")]
        public string SenderPhone { get; set; }

        [Required]
        [Display(Name = "Sender Address")]
        public string SenderAddress { get; set; }


        // =========================
        // Receiver Information
        // =========================

        [Required]
        [Display(Name = "Receiver Name")]
        public string ReceiverName { get; set; }

        [EmailAddress]
        [Display(Name = "Receiver Email")]
        public string ReceiverEmail { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Receiver Phone")]
        public string ReceiverPhone { get; set; }

        [Required]
        [Display(Name = "Receiver Address")]
        public string ReceiverAddress { get; set; }


        // =========================
        // Parcel Information
        // =========================

        [Required]
        [Display(Name = "Parcel Type")]
        public string ParcelType { get; set; }

        [Required]
        [Range(0.1, 100)]
        [Display(Name = "Parcel Weight (kg)")]
        public decimal ParcelWeight { get; set; }

        [Range(1, 200)]
        [Display(Name = "Length (cm)")]
        public decimal? Length { get; set; }

        [Range(1, 200)]
        [Display(Name = "Width (cm)")]
        public decimal? Width { get; set; }

        [Range(1, 200)]
        [Display(Name = "Height (cm)")]
        public decimal? Height { get; set; }

        [Display(Name = "Fragile Item")]
        public bool IsFragile { get; set; }


        // =========================
        // Delivery Information
        // =========================

        [Required]
        [Display(Name = "Delivery Option")]
        public string DeliveryOption { get; set; }

        [Required]
        [Display(Name = "Pickup Date & Time")]
        public DateTime PickupDateTime { get; set; }

        [Required]
        [Display(Name = "Pickup Address")]
        public string PickupAddress { get; set; }


        // =========================
        // System Fields
        // =========================
        public decimal CalculatedPrice { get; set; }
        public decimal EstimatedPrice { get; set; }


        // =========================
        // Dropdown Lists
        // =========================

        public List<SelectListItem> ParcelTypes { get; set; }

        public List<SelectListItem> DeliveryOptions { get; set; }


        // =========================
        // File Uploads
        // =========================

        [Display(Name = "Parcel Images")]
        public IEnumerable<HttpPostedFileBase> ParcelFiles { get; set; }
        public string SenderCity { get; set; }
        public string SenderProvince { get;set; }
        public string ReceiverCity { get; set; }
        public string ReceiverProvince { get; set; }
    }
}