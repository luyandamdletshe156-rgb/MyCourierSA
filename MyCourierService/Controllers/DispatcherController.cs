using Microsoft.AspNet.Identity;
using MyCourierSA.Constants;
using MyCourierSA.Models;
using MyCourierSA.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyCourierSA.Controllers
{
    [Authorize(Roles = AppConstants.Roles.RoleAdminDispatcher)]
    public class DispatcherController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly ShipmentEmailService _emailService; // <--- ADD THIS
        private readonly PricingService _pricingService;

        public DispatcherController()
        {
            db = new ApplicationDbContext();
            _emailService = new ShipmentEmailService(); // <--- INITIALIZE THIS
            _pricingService = new PricingService(db); // Initialized
        }


        // ==========================================
        // 1. DASHBOARD & MASTER LISTS
        // ==========================================
        // 2. Updated Index to handle "Active Logistics" correctly
        public async Task<ActionResult> Index(string status)
        {
            var statusCounts = await db.Shipments
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            var query = db.Shipments.Include(s => s.AssignedDriver).AsQueryable();

            // Default to "Active" if no status is passed
            if (string.IsNullOrEmpty(status)) status = "Active";

            switch (status)
            {
                case "Active":
                    // FIXED: "Active" now only shows shipments actually in motion (Picked up or In Transit)
                    // We EXCLUDE 'Approved' and 'At Warehouse' because they have their own cards
                    query = query.Where(s => s.Status == AppConstants.ShipmentStatuses.AssignedForPickup ||
                                             s.Status == AppConstants.ShipmentStatuses.Collected ||
                                             s.Status == AppConstants.ShipmentStatuses.AssignedForDelivery ||
                                             s.Status == AppConstants.ShipmentStatuses.InTransit);
                    break;

                case "Pending":
                    query = query.Where(s => s.Status == AppConstants.ShipmentStatuses.Pending);
                    break;

                case "Approved":
                    // This is your "Ready to Assign" bucket
                    query = query.Where(s => s.Status == AppConstants.ShipmentStatuses.Approved);
                    break;

                case "At Warehouse":
                    query = query.Where(s => s.Status == AppConstants.ShipmentStatuses.AtWarehouse);
                    break;

                default:
                    query = query.Where(s => s.Status == status);
                    break;
            }

            ViewBag.Drivers = await GetDriverSelectList();

            var model = new DispatcherDashboardViewModel
            {
                PendingCount = GetCount(statusCounts, AppConstants.ShipmentStatuses.Pending),
                ApprovedCount = GetCount(statusCounts, AppConstants.ShipmentStatuses.Approved),
                WarehouseCount = GetCount(statusCounts, AppConstants.ShipmentStatuses.AtWarehouse),

                // FIXED: Count only the shipments actually "In Motion"
                AssignedCount = GetCount(statusCounts, AppConstants.ShipmentStatuses.AssignedForPickup) +
                                GetCount(statusCounts, AppConstants.ShipmentStatuses.AssignedForDelivery) +
                                GetCount(statusCounts, AppConstants.ShipmentStatuses.InTransit) +
                                GetCount(statusCounts, AppConstants.ShipmentStatuses.Collected),

                CurrentFilter = status,
                AllShipments = await query.OrderByDescending(s => s.CreatedDate).Take(100).ToListAsync()
            };

            return View(model);
        }
        [HttpGet]
        public async Task<ActionResult> Details(int id)
        {
            var shipment = await db.Shipments
                .Include(s => s.AssignedDriver)
                .Include(s => s.ShipmentFiles)
                .Include(s => s.StatusHistories)
                .FirstOrDefaultAsync(s => s.Id == id);
            return shipment == null ? (ActionResult)HttpNotFound() : View(shipment);
        }

        // ==========================================
        // 2. DISPATCHER APPROVAL & CANCELLATION
        // ==========================================
        // In DispatcherController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ApprovePickup(int id)
        {
            var shipment = await db.Shipments.FindAsync(id);
            if (shipment == null) return HttpNotFound();

            // Ensure it stops here!
            shipment.Status = AppConstants.ShipmentStatuses.Approved;

            db.StatusHistories.Add(CreateHistoryRecord(shipment.Id, shipment.Status, "Dispatcher Approved. Waiting for Driver Assignment."));
            await db.SaveChangesAsync();

            TempData["Success"] = "Shipment approved. It is now in the 'Ready to Assign' queue.";
            return RedirectToAction("Index", new { status = AppConstants.ShipmentStatuses.Approved });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelShipment(int id, string reason)
        {
            var shipment = await db.Shipments.FindAsync(id);
            if (shipment == null) return HttpNotFound();

            shipment.Status = AppConstants.ShipmentStatuses.Cancelled;
            db.StatusHistories.Add(CreateHistoryRecord(shipment.Id, shipment.Status, "Dispatcher Voided Shipment. Reason: " + reason));

            await db.SaveChangesAsync();

            // TRIGGER EMAIL
            try
            {
                await _emailService.SendStatusUpdateEmailAsync(shipment, shipment.SenderEmail, "Cancelled");
            }
            catch { /* Log error */ }

            TempData["Error"] = $"Shipment {shipment.TrackingNumber} has been cancelled and customer notified.";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 3. WAREHOUSE OPS (Receive & Label)
        // ==========================================
        [HttpGet]
        public async Task<ActionResult> ProcessAtWarehouse(int id)
        {
            var shipment = await db.Shipments.FindAsync(id);
            if (shipment == null) return HttpNotFound();

            bool isInnerCity = string.Equals(shipment.SenderCity, shipment.ReceiverCity, StringComparison.OrdinalIgnoreCase);
            ViewBag.SuggestedBin = isInnerCity ? $"LOCAL-{shipment.ReceiverCity?.ToUpper()}" : $"NAT-{shipment.ReceiverProvince?.ToUpper()}";
            ViewBag.IsSameCity = isInnerCity;

            return View(shipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ProcessAtWarehouse(int id, string condition, string sortingBin)
        {
            var shipment = await db.Shipments.FindAsync(id);
            if (shipment == null) return HttpNotFound();

            shipment.Condition = condition;
            shipment.SortingBin = sortingBin.ToUpper();
            shipment.Status = AppConstants.ShipmentStatuses.AtWarehouse;

            db.StatusHistories.Add(CreateHistoryRecord(shipment.Id, shipment.Status, $"Warehouse Received. Condition: {condition}. Bin: {sortingBin}"));

            await db.SaveChangesAsync();

            // TRIGGER EMAIL
            try
            {
                await _emailService.SendStatusUpdateEmailAsync(shipment, shipment.SenderEmail, shipment.Status);
            }
            catch { /* Log error */ }

            return RedirectToAction("PrintLabel", new { id = shipment.Id });
        }

        [HttpGet]
        public async Task<ActionResult> PrintLabel(int id)
        {
            var shipment = await db.Shipments.FindAsync(id);
            return shipment == null ? (ActionResult)HttpNotFound() : View(shipment);
        }

        // ==========================================
        // 4. ROUTE PLANNING (Grouping)
        // ==========================================
        [HttpGet]
        public async Task<ActionResult> RoutePlanner()
        {
            // 1. Start the query for items at the warehouse
            var warehouseItemsQuery = db.Shipments.Where(s => s.Status == AppConstants.ShipmentStatuses.AtWarehouse);

            // 2. Group and Select directly into the ViewModel
            var groups = await warehouseItemsQuery
                .GroupBy(s => new { s.ReceiverProvince, s.ReceiverCity, s.SenderCity, s.SortingBin })
                .Select(g => new RouteBatchViewModel
                {
                    Province = g.Key.ReceiverProvince,
                    City = g.Key.ReceiverCity,
                    SuggestedBin = g.Key.SortingBin ?? "UNSORTED",
                    TotalParcels = g.Count(),
                    TotalWeight = g.Sum(s => s.ParcelWeight),

                    // Perform the comparison here in the query
                    IsLocal = g.Key.SenderCity == g.Key.ReceiverCity,

                    Shipments = g.ToList()
                }).ToListAsync();

            var dashboard = new RoutePlannerDashboardViewModel();

            // 3. Sort the results into the two dashboard lists
            foreach (var g in groups)
            {
                if (g.IsLocal)
                    dashboard.InnerCityBatches.Add(g);
                else
                    dashboard.OuterCityBatches.Add(g);
            }

            dashboard.AvailableDrivers = await GetDriverSelectList();
            return View(dashboard);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AssignRouteBatch(string province, string city, string driverId)
        {
            if (string.IsNullOrEmpty(driverId) || string.IsNullOrEmpty(province) || string.IsNullOrEmpty(city))
            {
                TempData["Error"] = "Invalid route or driver data provided.";
                return RedirectToAction("RoutePlanner");
            }

            var driver = await db.Users.FirstOrDefaultAsync(u => u.Id == driverId);
            if (driver == null)
            {
                TempData["Error"] = "Selected driver could not be found.";
                return RedirectToAction("RoutePlanner");
            }

            // This query is now safe
            var shipments = await db.Shipments
                .Where(s => s.Status == AppConstants.ShipmentStatuses.AtWarehouse &&
                              s.ReceiverProvince == province &&
                              s.ReceiverCity == city)
                .ToListAsync();

            if (!shipments.Any())
            {
                TempData["Warning"] = "No pending shipments found for that specific route batch.";
                return RedirectToAction("RoutePlanner");
            }

            foreach (var s in shipments)
            {
                s.AssignedDriverId = driverId;
                s.Status = AppConstants.ShipmentStatuses.AssignedForDelivery;
                db.StatusHistories.Add(CreateHistoryRecord(s.Id, s.Status, $"Batch assigned to {driver.UserName}"));
            }
            await db.SaveChangesAsync();

            TempData["Success"] = $"Dispatched {shipments.Count} parcels for {city} to driver {driver.UserName}.";
            return RedirectToAction("RoutePlanner");
        }

        // ==========================================
        // 5. MANUAL ASSIGNMENT (Single)
        // GET: Dispatcher/AssignDriver/5
        [HttpGet]
        public async Task<ActionResult> AssignDriver(int id)
        {
            var shipment = await db.Shipments
                .Include(s => s.ShipmentFiles)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null) return HttpNotFound();

            // Determine if we are assigning for Pickup or Delivery
            ViewBag.AssignmentType = (shipment.Status == AppConstants.ShipmentStatuses.Approved) ? "Collection Pickup" : "Final Delivery";
            ViewBag.Drivers = await GetDriverSelectList();

            return View(shipment);
        }

        // POST: Dispatcher/AssignDriver
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AssignDriver(int id, string assignedDriverId)
        {
            var shipment = await db.Shipments.FindAsync(id);
            if (shipment == null) return HttpNotFound();

            shipment.AssignedDriverId = assignedDriverId;

            // If it was waiting for pickup, move to 'Assigned for Pickup'
            // If it was at warehouse, move to 'Assigned for Delivery'
            if (shipment.Status == AppConstants.ShipmentStatuses.Approved)
                shipment.Status = AppConstants.ShipmentStatuses.AssignedForPickup;
            else if (shipment.Status == AppConstants.ShipmentStatuses.AtWarehouse)
                shipment.Status = AppConstants.ShipmentStatuses.AssignedForDelivery;

            await db.SaveChangesAsync();
            return RedirectToAction("Index", new { status = "Active" }); // Now it moves to the Active card
        }
        // ==========================================
        // 6. WALK-IN SHIPMENT CREATION
        // ==========================================
        [HttpGet]
        public ActionResult CreateShipment() => View(new CreateShipmentViewModel { ParcelTypes = GetParcelTypes(), DeliveryOptions = GetDeliveryOptions(), PickupDateTime = DateTime.Now });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateShipment(CreateShipmentViewModel model, IEnumerable<HttpPostedFileBase> ParcelFiles)
        {
            if (!ModelState.IsValid)
            {
                model.ParcelTypes = GetParcelTypes();
                model.DeliveryOptions = GetDeliveryOptions();
                return View(model);
            }

            try
            {
                // 1. Calculate Price
                decimal price = await _pricingService.CalculatePriceAsync(model.ParcelWeight, model.ParcelType, model.DeliveryOption);

                // 2. Map ViewModel to Shipment Entity
                var shipment = new Shipment
                {
                    CustomerId = null, // Walk-in (Paid via cash/card at desk)
                    SenderName = model.SenderName,
                    SenderEmail = model.SenderEmail ?? "walkin@mycourier.co.za",
                    SenderPhone = model.SenderPhone ?? "0000000000",
                    SenderAddress = model.SenderAddress,
                    SenderCity = model.SenderCity,
                    SenderProvince = model.SenderProvince,
                    ReceiverName = model.ReceiverName,
                    ReceiverEmail = model.ReceiverEmail,
                    ReceiverPhone = model.ReceiverPhone,
                    ReceiverAddress = model.ReceiverAddress,
                    ReceiverCity = model.ReceiverCity,
                    ReceiverProvince = model.ReceiverProvince,
                    ParcelType = model.ParcelType,
                    ParcelWeight = model.ParcelWeight,
                    DeliveryOption = model.DeliveryOption,
                    Status = AppConstants.ShipmentStatuses.Approved, // Auto-approve for walk-ins
                    TrackingNumber = "MC" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    Price = price,
                    CreatedDate = DateTime.UtcNow,
                    PickupAddress = "Service Center - Walk In",
                    PickupDateTime = DateTime.UtcNow
                };

                db.Shipments.Add(shipment);

                // 3. Add History Record
                db.StatusHistories.Add(new StatusHistory
                {
                    Shipment = shipment,
                    Status = shipment.Status,
                    Notes = "Walk-in Induction (Paid at Counter).",
                    Timestamp = DateTime.UtcNow,
                    UpdatedById = User.Identity.GetUserId()
                });

                // 4. Handle File Uploads
                if (ParcelFiles != null)
                {
                    foreach (var file in ParcelFiles.Where(f => f != null && f.ContentLength > 0))
                    {
                        using (var ms = new MemoryStream())
                        {
                            await file.InputStream.CopyToAsync(ms);
                            db.ShipmentFiles.Add(new ShipmentFile
                            {
                                Shipment = shipment,
                                FileName = Path.GetFileName(file.FileName),
                                ContentType = file.ContentType,
                                Content = ms.ToArray()
                            });
                        }
                    }
                }

                // 5. Save to Database
                await db.SaveChangesAsync();

                // ==========================================
                // NEW: SEND EMAIL TO WALK-IN CUSTOMER
                // ==========================================
                try
                {
                    // Send the shipment details to the sender's email
                    await _emailService.SendShipmentCreatedEmailAsync(shipment, shipment.SenderEmail);
                }
                catch (Exception emailEx)
                {
                    // Log error to debug console but allow the dispatcher to continue
                    System.Diagnostics.Debug.WriteLine("Walk-in Email failed: " + emailEx.Message);
                }
                // ==========================================

                TempData["Success"] = $"Walk-in Shipment {shipment.TrackingNumber} created successfully!";
                return RedirectToAction("Index", new { status = AppConstants.ShipmentStatuses.Approved });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Creation failed: " + ex.Message);
                model.ParcelTypes = GetParcelTypes();
                model.DeliveryOptions = GetDeliveryOptions();
                return View(model);
            }
        }
        // GET: Dispatcher/ManageShipments
        public async Task<ActionResult> ManageShipments()
        {
            // Fetch all shipments, most recent first
            var shipments = await db.Shipments
                .Include(s => s.AssignedDriver) // Include driver info if you have the relationship
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            return View(shipments);
        }

        // ==========================================
        // 7. FILE UTILITIES
        // ==========================================
        [HttpGet]
        public async Task<ActionResult> GetShipmentImage(int fileId)
        {
            var file = await db.ShipmentFiles.FindAsync(fileId);
            return (file == null || !file.ContentType.StartsWith("image/")) ? (ActionResult)HttpNotFound() : File(file.Content, file.ContentType);
        }

        [HttpGet]
        public async Task<ActionResult> DownloadFile(int fileId)
        {
            var file = await db.ShipmentFiles.FindAsync(fileId);
            if (file == null) return HttpNotFound();
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpGet]
        public async Task<ActionResult> TrackShipment(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber)) return View();
            var shipment = await db.Shipments.Include(s => s.AssignedDriver).Include(s => s.StatusHistories).FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber.Trim());
            if (shipment == null) { ViewBag.Error = "Tracking number not found."; return View(); }
            return View(shipment);
        }

        // ==========================================
        // HELPERS (REFACTORED)
        // ==========================================
        private async Task<SelectList> GetDriverSelectList()
        {
            var driverRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == AppConstants.Roles.Driver);
            if (driverRole == null) return new SelectList(new List<ApplicationUser>());

            var drivers = await db.Users
                .Where(u => u.Roles.Any(r => r.RoleId == driverRole.Id))
                .Select(u => new { u.Id, Display = u.Name + " " + u.Surname + " (" + u.UserName + ")" })
                .ToListAsync();

            return new SelectList(drivers, "Id", "Display");
        }

        private int GetCount(Dictionary<string, int> dict, string key) => dict.ContainsKey(key) ? dict[key] : 0;

        private StatusHistory CreateHistoryRecord(int shipmentId, string status, string notes) => new StatusHistory
        {
            ShipmentId = shipmentId,
            Status = status,
            Notes = notes,
            Timestamp = DateTime.UtcNow,
            UpdatedById = User.Identity.GetUserId()
        };

        private List<SelectListItem> GetParcelTypes() => new List<SelectListItem> {
            new SelectListItem { Text = "Small Package", Value = AppConstants.ParcelTypes.Small },
            new SelectListItem { Text = "Medium Package", Value = AppConstants.ParcelTypes.Medium },
            new SelectListItem { Text = "Large Package", Value = AppConstants.ParcelTypes.Large }
        };

        private List<SelectListItem> GetDeliveryOptions() => new List<SelectListItem> {
            new SelectListItem { Text = "Standard", Value = AppConstants.DeliveryOptions.Standard },
            new SelectListItem { Text = "Express", Value = AppConstants.DeliveryOptions.Express },
            new SelectListItem { Text = "Overnight", Value = AppConstants.DeliveryOptions.Overnight }
        };

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}