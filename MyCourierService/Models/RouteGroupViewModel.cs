using MyCourierSA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc; // Required for SelectList

namespace MyCourierSA.Models
{
    // 1. This handles the entire Route Planner Dashboard screen
    public class RoutePlannerDashboardViewModel
    {
        // Fast-track inner-city deliveries
        public List<RouteBatchViewModel> InnerCityBatches { get; set; } = new List<RouteBatchViewModel>();

        // Standard outer-city / regional deliveries
        public List<RouteBatchViewModel> OuterCityBatches { get; set; } = new List<RouteBatchViewModel>();

        // Dropdown list of available drivers
        public SelectList AvailableDrivers { get; set; }
    }

    // 2. This is your upgraded RouteGroupViewModel (renamed to RouteBatchViewModel to match the Controller)
    public class RouteBatchViewModel
    {
        public string Province { get; set; }
        public string City { get; set; }
        public string SuggestedBin { get; set; }
        public int TotalParcels { get; set; }
        public decimal TotalWeight { get; set; }

        // ADD THIS LINE:
        public bool IsLocal { get; set; }

        // Ensure this matches what your View expects 
        // (If your view uses s.Shipments, keep it as List<Shipment>)
        public List<Shipment> Shipments { get; set; }
    }
}