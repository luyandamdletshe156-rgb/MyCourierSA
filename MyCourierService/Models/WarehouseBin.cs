using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyCourierSA.Models
{
    public class WarehouseBin
    { 
    public int Id { get; set; }
        public string BinCode { get; set; } // e.g., "A1", "B2"
        public bool IsOccupied { get; set; }
        public int? CurrentShipmentId { get; set; }
    }
}
