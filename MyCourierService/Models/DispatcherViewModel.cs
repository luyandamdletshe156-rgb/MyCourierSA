using MyCourierSA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyCourierSA.Models
{
    public class DispatcherDashboardViewModel
    {
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int AssignedCount { get; set; }
        public List<Shipment> AllShipments { get; set; }
        public SelectList Drivers { get; set; }
        public List<Shipment> UrgentShipments { get; set; }
        public string CurrentFilter { get; internal set; }
        public int WarehouseCount { get; internal set; }
    }
}