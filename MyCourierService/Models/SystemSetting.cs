using System.ComponentModel.DataAnnotations;

namespace MyCourierSA.Models
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } // e.g., "BaseFee"

        [Required]
        public string Value { get; set; } // e.g., "40.00"

        public string Description { get; set; }
    }
}