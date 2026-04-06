// Updated ShipmentFile.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCourierSA.Models
{
    public class ShipmentFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; }

        // REMOVE [Required] if you are storing the file in the database as byte[]
        public string FilePath { get; set; }

        public DateTime UploadedAt { get; set; }

        [Required]
        public int ShipmentId { get; set; }
        [ForeignKey("ShipmentId")]
        public virtual Shipment Shipment { get; set; }

        public string ContentType { get; set; }
        public byte[] Content { get; set; }

        public ShipmentFile()
        {
            UploadedAt = DateTime.Now;
        }
    }
}