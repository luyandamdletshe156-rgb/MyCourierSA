using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MyCourierSA.Models;

namespace MyCourierSA.Constants
{
    public static class AppConstants
    {
        // ==========================================
        // USER ROLES
        // ==========================================
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string Dispatcher = "Dispatcher";
            public const string Driver = "Driver";
            public const string Customer = "Customer";

            // Combined string for [Authorize] attributes
            public const string RoleAdminDispatcher = Admin + "," + Dispatcher;
        }


        public static class TransactionTypes
        {
            public const string Deposit = "Deposit";
            public const string Payment = "Payment";
            public const string Refund = "Refund";
        }

        // ==========================================
        // SHIPMENT STATUSES (The Lifecycle)
        // ==========================================
        public static class ShipmentStatuses
        {
            public const string Pending = "Pending";                       // Customer submitted, waiting for approval
            public const string Approved = "Approved";                     // Dispatcher authorized the pickup
            public const string AssignedForPickup = "Assigned for Collection"; // Driver assigned to fetch from sender
            public const string Collected = "Collected";                   // Driver picked up from sender
            public const string AtWarehouse = "At Warehouse";               // Received and sorted at Warehouse
            public const string AssignedForDelivery = "Assigned for Delivery"; // Driver assigned for final leg
            public const string InTransit = "In Transit";                  // Driver is on the way to receiver
            public const string Delivered = "Delivered";                   // Final destination reached
            public const string Cancelled = "Cancelled";                   // Voided shipment
        }

        // ==========================================
        // DELIVERY SPEED OPTIONS
        // ==========================================
        public static class DeliveryOptions
        {
            public const string Standard = "Standard";
            public const string Express = "Express";
            public const string Overnight = "Overnight";

            // Multipliers for Pricing Logic
            public const decimal MultiplierStandard = 1.0m;
            public const decimal MultiplierExpress = 1.5m;
            public const decimal MultiplierOvernight = 2.0m;
        }

        // ==========================================
        // PARCEL CATEGORIES
        // ==========================================
        public static class ParcelTypes
        {
            public const string Document = "Document";
            public const string Small = "Small Package";
            public const string Medium = "Medium Package";
            public const string Large = "Large Package";
        }

        // ==========================================
        // SHARED PRICING LOGIC CONFIG
        // ==========================================
        // Change these values here to update the whole system's pricing instantly
        public static class Pricing
        {
            public const decimal BaseFee = 40.0m;
            public const decimal PerKgRate = 12.0m;
            public const decimal SizeFeeSmall = 10.0m;
            public const decimal SizeFeeMedium = 20.0m;
            public const decimal SizeFeeLarge = 40.0m;
        }
    }
}