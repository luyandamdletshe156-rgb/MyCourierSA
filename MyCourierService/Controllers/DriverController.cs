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
    [Authorize(Roles = AppConstants.Roles.Driver)]
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly ShipmentEmailService _emailService; // <--- ADD THIS

        public DriverController()
        {

            db = new ApplicationDbContext();
            _emailService = new ShipmentEmailService(); // <--- INITIALIZE THIS
        }

        public DriverController(ApplicationDbContext context)
        {
            db = context;
        }

        private string CurrentUserId => User.Identity.GetUserId();

        // ==========================================
        // 1. DRIVER DASHBOARD & ROUTE
        // ==========================================
        public async Task<ActionResult> Index()
        {
            var driverId = CurrentUserId;
            var today = DateTime.UtcNow.Date;

            // 1. Fetch Active Tasks (Anything not yet delivered or cancelled)
            var activeStatuses = new[]
            {
        AppConstants.ShipmentStatuses.AssignedForPickup,
        AppConstants.ShipmentStatuses.Collected,
        AppConstants.ShipmentStatuses.AssignedForDelivery,
        AppConstants.ShipmentStatuses.InTransit
    };

            var activeTasks = await db.Shipments
                .Where(s => s.AssignedDriverId == driverId && activeStatuses.Contains(s.Status))
                .OrderBy(s => s.PickupDateTime)
                .ToListAsync();

            // 2. Calculate Real-Time Earnings from Wallet (Pickups + Deliveries today)
            ViewBag.DailyEarnings = await db.WalletTransactions
                .Where(t => t.UserId == driverId
                         && t.TransactionType == "Earning"
                         && DbFunctions.TruncateTime(t.Timestamp) == today)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            // 3. Stats for Widgets
            ViewBag.PickupCount = activeTasks.Count(s => s.Status == AppConstants.ShipmentStatuses.AssignedForPickup);
            ViewBag.DeliveryCount = activeTasks.Count(s => s.Status == AppConstants.ShipmentStatuses.AssignedForDelivery || s.Status == AppConstants.ShipmentStatuses.InTransit);

            // 4. Recently Completed Today (Last 3 shipments)
            ViewBag.PastDeliveries = await db.Shipments
                .Where(s => s.AssignedDriverId == driverId
                         && s.Status == AppConstants.ShipmentStatuses.Delivered
                         && DbFunctions.TruncateTime(s.EstimatedDeliveryDate) == today)
                .OrderByDescending(s => s.EstimatedDeliveryDate)
                .Take(3)
                .ToListAsync();

            return View(activeTasks);
        }
        // ==========================================
        // 2. PARCEL COLLECTION
        // ==========================================




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmCollection(int id)
        {
            var shipment = await GetAssignedShipmentSafeAsync(id);
            if (shipment == null) return HttpNotFound();

            if (shipment.Status != AppConstants.ShipmentStatuses.AssignedForPickup)
                return RedirectToAction("Index");

            try
            {
                // 1. Update Shipment Status
                shipment.Status = AppConstants.ShipmentStatuses.Collected;

                // 2. Add History (FIXED: Added real data instead of placeholders)
                db.StatusHistories.Add(new StatusHistory
                {
                    ShipmentId = shipment.Id,
                    Status = shipment.Status,
                    Timestamp = DateTime.UtcNow,
                    Notes = "Driver confirmed parcel collection.",
                    UpdatedById = CurrentUserId
                });

                // 3. Pay Driver
                decimal collectionRate = await GetCommissionSettingAsync("CollectionCommissionRate", 0.30m);
                decimal earnings = shipment.Price * collectionRate;

                // This method calls SaveChangesAsync inside it
                await db.AdjustWalletBalance(CurrentUserId, earnings,
                    $"Pickup Commission: {shipment.TrackingNumber}", "Earning");

                TempData["Success"] = "Parcel collected and commission earned!";
                return RedirectToAction("Index");
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // DIAGNOSTIC: This loop finds the EXACT field that is failing
                foreach (var failure in ex.EntityValidationErrors)
                {
                    foreach (var error in failure.ValidationErrors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Property: {error.PropertyName} Error: {error.ErrorMessage}");
                        ModelState.AddModelError("", $"Property: {error.PropertyName} Error: {error.ErrorMessage}");
                    }
                }
                return View("Error"); // Or return to Index with TempData["Error"]
            }
        }
        // ==========================================
        // 2.5 DROP AT WAREHOUSE (After Collection)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DropAtWarehouse(int id)
        {
            var shipment = await GetAssignedShipmentSafeAsync(id);
            if (shipment == null) return HttpNotFound();

            if (shipment.Status != AppConstants.ShipmentStatuses.Collected)
            {
                TempData["Error"] = "Parcel is not ready to be dropped at the warehouse.";
                return RedirectToAction("Index");
            }

            // Update status to AtWarehouse and remove it from the driver's current route
            shipment.Status = AppConstants.ShipmentStatuses.AtWarehouse;
            shipment.AssignedDriverId = null; // The driver's job for this leg is done

            db.StatusHistories.Add(new StatusHistory
            {
                ShipmentId = shipment.Id,
                Status = shipment.Status,
                Notes = "Driver dropped off the collected parcel at the warehouse.",
                Timestamp = DateTime.UtcNow,
                UpdatedById = CurrentUserId
            });

            await db.SaveChangesAsync();
            TempData["Success"] = $"Parcel {shipment.TrackingNumber} successfully dropped at warehouse.";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 3. EN ROUTE & FINAL DELIVERY
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StartDelivery(int id)
        {
            var shipment = await GetAssignedShipmentSafeAsync(id);
            if (shipment == null) return HttpNotFound();

            // ...
            shipment.Status = AppConstants.ShipmentStatuses.InTransit;
            // ...

            await db.SaveChangesAsync();

            // ==========================================
            // TRIGGER EMAIL: IN TRANSIT / OUT FOR DELIVERY
            // ==========================================
            try
            {
                await _emailService.SendStatusUpdateEmailAsync(shipment, shipment.SenderEmail, shipment.Status);
            }
            catch { /* Log error */ }

            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<ActionResult> CompleteDelivery(int id)
        {
            var shipment = await GetAssignedShipmentSafeAsync(id);
            if (shipment == null) return HttpNotFound();

            return View(shipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CompleteDelivery(int id, string recipientName, HttpPostedFileBase proofOfDeliveryFile)
        {
            var shipment = await GetAssignedShipmentSafeAsync(id);
            if (shipment == null) return HttpNotFound();

            // 1. Basic Validations
            if (shipment.Status == AppConstants.ShipmentStatuses.Delivered)
            {
                TempData["Info"] = "This shipment has already been delivered.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(recipientName))
            {
                ModelState.AddModelError("recipientName", "The name of the person receiving the parcel is required.");
            }

            if (proofOfDeliveryFile == null || proofOfDeliveryFile.ContentLength == 0)
            {
                ModelState.AddModelError("", "Proof of Delivery (Photo or Signature scan) is required.");
            }

            if (!ModelState.IsValid) return View(shipment);

            // 2. File Extension Check
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var fileExtension = Path.GetExtension(proofOfDeliveryFile.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("", "Invalid file format. Only JPG, PNG, and PDF are allowed.");
                return View(shipment);
            }

            // 3. Process completion within a Transaction
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // A. Handle File Upload
                    string fileName = $"POD_{shipment.TrackingNumber}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                    string folderPath = Server.MapPath("~/Uploads/POD/");

                    // Ensure folder exists
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    string fullPath = Path.Combine(folderPath, fileName);
                    proofOfDeliveryFile.SaveAs(fullPath);

                    // B. Update Shipment Record
                    shipment.Status = AppConstants.ShipmentStatuses.Delivered;
                    shipment.ReceiverName = recipientName; // Update with the actual person who signed
                    shipment.EstimatedDeliveryDate = DateTime.UtcNow;
                    shipment.ProofOfDeliveryPath = "/Uploads/POD/" + fileName;

                    // C. Create Audit History
                    db.StatusHistories.Add(new StatusHistory
                    {
                        ShipmentId = shipment.Id,
                        Status = AppConstants.ShipmentStatuses.Delivered,
                        Timestamp = DateTime.UtcNow,
                        Notes = $"Handed over to: {recipientName}. POD recorded.",
                        UpdatedById = CurrentUserId
                    });

                    // D. CALCULATE & PAY DRIVER (Commission)
                    // Fetch rate from settings (Default to 40% if not set)
                    decimal deliveryRate = await GetCommissionSettingAsync("DeliveryCommissionRate", 0.40m);
                    decimal earnings = shipment.Price * deliveryRate;

                    // Update Driver Wallet Balance and Log Transaction
                    await db.AdjustWalletBalance(
                        CurrentUserId,
                        earnings,
                        $"Delivery Commission: {shipment.TrackingNumber}",
                        "Earning"
                    );

                    // E. Save all changes and Commit
                    await db.SaveChangesAsync();
                    transaction.Commit();

                    // 4. Trigger Email Notification (Outside transaction)
                    try
                    {
                        await _emailService.SendStatusUpdateEmailAsync(shipment, shipment.SenderEmail, shipment.Status);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Delivery Email Failed: " + ex.Message);
                    }

                    TempData["Success"] = $"Delivery Completed! R{earnings:N2} has been added to your wallet.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine("CRITICAL ERROR in CompleteDelivery: " + ex.Message);
                    ModelState.AddModelError("", "A system error occurred while processing the delivery. Please try again.");
                    return View(shipment);
                }
            }
        }
        [HttpGet]
        public async Task<ActionResult> ReportIssue(int id)
        {
            var shipment = await GetAssignedShipmentSafeAsync(id);
            if (shipment == null) return HttpNotFound();

            return View(shipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReportIssue(int id, string reason)
        {
            var shipment = await GetAssignedShipmentSafeAsync(id);
            if (shipment == null) return HttpNotFound();

            shipment.Status = AppConstants.ShipmentStatuses.AtWarehouse;
            shipment.AssignedDriverId = null;

            db.StatusHistories.Add(new StatusHistory
            {
                ShipmentId = shipment.Id,
                Status = "Delivery Failed",
                Notes = $"Driver reported issue: {reason}. Returning to warehouse.",
                Timestamp = DateTime.UtcNow,
                UpdatedById = CurrentUserId
            });

            await db.SaveChangesAsync();

            // TRIGGER EMAIL
            try
            {
                await _emailService.SendStatusUpdateEmailAsync(shipment, shipment.SenderEmail, "Delivery Attempt Failed");
            }
            catch { }

            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. EARNINGS & HISTORY
        // ==========================================
        public async Task<ActionResult> Earnings()
        {
            var driverId = CurrentUserId;
            var now = DateTime.UtcNow;

            // 1. Get the Driver's actual Current Wallet Balance
            var driver = await db.Users.FirstOrDefaultAsync(u => u.Id == driverId);
            ViewBag.CurrentBalance = driver?.WalletBalance ?? 0m;

            // 2. Fetch all "Earning" transactions for this month
            var monthlyEarningsQuery = db.WalletTransactions
                .Where(t => t.UserId == driverId
                         && t.TransactionType == "Earning"
                         && t.Timestamp.Month == now.Month
                         && t.Timestamp.Year == now.Year);

            // Calculate Monthly Stats
            ViewBag.MonthlyEarnings = await monthlyEarningsQuery.SumAsync(t => (decimal?)t.Amount) ?? 0m;
            ViewBag.MonthlyTaskCount = await monthlyEarningsQuery.CountAsync();

            // 3. Calculate Today's Earnings (for the dashboard "Daily Goal")
            ViewBag.TodayEarnings = await db.WalletTransactions
                .Where(t => t.UserId == driverId
                         && t.TransactionType == "Earning"
                         && t.Timestamp.Day == now.Day
                         && t.Timestamp.Month == now.Month
                         && t.Timestamp.Year == now.Year)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            // 4. Fetch Transaction History (The most recent 50 earnings)
            // This will show separate lines for "Pickup Commission" and "Delivery Commission"
            var earningsHistory = await db.WalletTransactions
                .Where(t => t.UserId == driverId && t.TransactionType == "Earning")
                .OrderByDescending(t => t.Timestamp)
                .Take(50)
                .ToListAsync();

            // 5. Fetch Commission Rates for display in the UI (Optional)
            ViewBag.PickupRate = (await GetCommissionSettingAsync("CollectionCommissionRate", 0.30m)) * 100;
            ViewBag.DeliveryRate = (await GetCommissionSettingAsync("DeliveryCommissionRate", 0.40m)) * 100;

            return View(earningsHistory);
        }
        // ==========================================
        // HELPERS
        // ==========================================

        private async Task<Shipment> GetAssignedShipmentSafeAsync(int shipmentId)
        {
            var driverId = CurrentUserId;
            return await db.Shipments
                .FirstOrDefaultAsync(s => s.Id == shipmentId && s.AssignedDriverId == driverId);
        }

        // NEW: Shared helper to get driver commission securely
        // This helper works for ANY commission key (Pickup, Delivery, or the old General rate)
        private async Task<decimal> GetCommissionSettingAsync(string key, decimal defaultValue)
        {
            var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null) return defaultValue;

            // Convert string "0.70" or "0,70" to decimal 0.70
            string rawValue = setting.Value.Replace(",", ".").Trim();
            if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rate))
                return rate;

            return defaultValue;
        }

        // Keep this name if your Index() or other methods still call it for 'General' calculations
        private async Task<decimal> GetDriverCommissionRateAsync()
        {
            // This calls the generic helper using the old "DriverCommissionRate" key
            return await GetCommissionSettingAsync("DriverCommissionRate", 0.70m);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}