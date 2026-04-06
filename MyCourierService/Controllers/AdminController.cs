using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MyCourierSA.Constants;
using MyCourierSA.Models;
using MyCourierSA.Services;
using PagedList;
using PagedList.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyCourierSA.Controllers
{
    [Authorize(Roles = AppConstants.Roles.Admin)]
    public class AdminController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private readonly PricingService _pricingService;
        private readonly ShipmentEmailService _emailService;
        private ApplicationUserManager _userManager;
        public AdminController()
        {
            db = new ApplicationDbContext();
            _pricingService = new PricingService(db);
            _emailService = new ShipmentEmailService();
        }

        public ApplicationUserManager UserManager => _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();

        // ==========================================
        // 1. EXECUTIVE COMMAND CENTER (Index)
        // ==========================================
        public async Task<ActionResult> Index()
        {
            // 1. Total Delivered Revenue (The money the company has successfully earned)
            ViewBag.TotalRevenue = await db.Shipments
                .Where(s => s.Status == AppConstants.ShipmentStatuses.Delivered)
                .SumAsync(s => (decimal?)s.Price) ?? 0m;

            // 2. DRIVER FLEET LIABILITY (Money sitting in driver wallets that hasn't been paid out yet)
            var driverRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == AppConstants.Roles.Driver);
            string driverRoleId = driverRole?.Id;

            ViewBag.DriverLiability = await db.Users
                .Where(u => u.Roles.Any(r => r.RoleId == driverRoleId))
                .SumAsync(u => (decimal?)u.WalletBalance) ?? 0m;

            // 3. INSURANCE CLAIMS RISK (The total value of shipments linked to Open or In-Progress support tickets)
            // This represents the maximum potential payout for current customer disputes.
            ViewBag.ClaimsLiability = await db.SupportTickets
                .Where(t => t.ShipmentId != null && (t.Status == "Open" || t.Status == "In Progress"))
                .SumAsync(t => (decimal?)t.RelatedShipment.Price) ?? 0m;

            // 4. Monthly Payouts (Cash that actually left the business to pay drivers this month)
            ViewBag.MonthlyPayouts = await db.WalletTransactions
                .Where(t => t.TransactionType == "Withdrawal" && t.Timestamp.Month == DateTime.Now.Month)
                .SumAsync(t => (decimal?)Math.Abs(t.Amount)) ?? 0m;

            // General Stats
            ViewBag.TotalShipments = await db.Shipments.CountAsync();
            ViewBag.OpenTickets = await db.SupportTickets.CountAsync(t => t.Status == "Open" || t.Status == "In Progress");

            var recentShipments = await db.Shipments
                .Include(s => s.AssignedDriver)
                .OrderByDescending(s => s.CreatedDate)
                .Take(10)
                .ToListAsync();

            return View(recentShipments);
        }
       
        // 2. PERSONNEL MANAGEMENT
        // ==========================================
        public async Task<ActionResult> Users()
        {
            var users = await db.Users.ToListAsync();
            var roles = await db.Roles.ToListAsync();

            var userList = users.Select(u => new UserViewModel
            {
                Id = u.Id,
                FullName = $"{u.Name} {u.Surname}",
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                WalletBalance = u.WalletBalance,
                IsActive = u.IsActive, // ADD THIS LINE TO MAP THE DATA
                Roles = u.Roles.Join(roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name).ToList()
            }).OrderByDescending(u => u.CreatedAt).ToList();

            return View(userList);
        }
        // GET: Admin/CreateStaff
        [HttpGet]
        public ActionResult CreateStaff()
        {
            // Populate the dropdown for the View
            ViewBag.Roles = new SelectList(new[] {
        AppConstants.Roles.Dispatcher,
        AppConstants.Roles.Driver
         });

            return View(new CreateStaffViewModel());
        }

        // POST: Admin/CreateStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateStaff(CreateStaffViewModel model, string selectedRole)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(selectedRole))
                {
                    ModelState.AddModelError("selectedRole", "Please select a clearance level (Role).");
                }
                else
                {
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        Name = model.Name,
                        Surname = model.Surname,
                        PhoneNumber = model.PhoneNumber,
                        CreatedAt = DateTime.UtcNow,
                        WalletBalance = 0,
                        IsActive = true
                    };

                    // Use the password from the model before it gets hashed
                    var result = await UserManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        await UserManager.AddToRoleAsync(user.Id, selectedRole);

                        // ==========================================
                        // NEW: SEND WELCOME EMAIL TO STAFF
                        // ==========================================
                        try
                        {
                            // Pass the user object, the plain text password, and the role name
                            await _emailService.SendWelcomeEmailAsync(user, model.Password, selectedRole);
                        }
                        catch (Exception ex)
                        {
                            // Log the error but don't stop the admin flow
                            System.Diagnostics.Debug.WriteLine("Staff Welcome Email Failed: " + ex.Message);
                        }
                        // ==========================================

                        TempData["Success"] = $"Account created for {model.Name} and credentials sent to {model.Email}.";
                        return RedirectToAction("Users", "Admin");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
            }

            ViewBag.Roles = new SelectList(new[] { AppConstants.Roles.Dispatcher, AppConstants.Roles.Driver }, selectedRole);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ToggleUserStatus(string id)
        {
            var user = await UserManager.FindByIdAsync(id);

            if (user == null) return HttpNotFound();

            // 1. SECURITY: Prevent Admin from deactivating themselves
            if (user.Id == User.Identity.GetUserId())
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction("Users");
            }

            // 2. TOGGLE LOGIC
            user.IsActive = !user.IsActive;
            user.DeactivatedAt = user.IsActive ? (DateTime?)null : DateTime.UtcNow;

            var result = await UserManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                string status = user.IsActive ? "Reactivated" : "Deactivated";
                TempData["Success"] = $"User {user.Name} {user.Surname} has been {status}.";
            }

            return RedirectToAction("Users");
        }
        // ==========================================
        // 3. FINANCIAL INTELLIGENCE
        // ==========================================
        public async Task<ActionResult> RevenueReport()
        {
            int currentYear = DateTime.UtcNow.Year;
            var data = await db.Shipments
                .Where(s => s.Status == AppConstants.ShipmentStatuses.Delivered && s.CreatedDate.Year == currentYear)
                .GroupBy(s => s.CreatedDate.Month)
                .Select(g => new MonthlyRevenueData
                {
                    Month = g.Key,
                    Total = g.Sum(s => (decimal)s.Price)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            ViewBag.OnTimeRate = 98.4; // Simulated KPI
            return View(data);
        }

        public async Task<ActionResult> DriverPayouts()
        {
            var driverRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == AppConstants.Roles.Driver);

            // Fetch all users in the Driver role and their current balances
            var payoutsData = await db.Users
                .Where(u => u.Roles.Any(r => r.RoleId == driverRole.Id))
                .Select(u => new DriverPayoutViewModel
                {
                    DriverId = u.Id,
                    FullName = u.Name + " " + u.Surname,
                    Email = u.Email,
                    CurrentBalance = u.WalletBalance,
                    // Fetch total earned this month for context
                    TotalEarnedThisMonth = db.WalletTransactions
                        .Where(t => t.UserId == u.Id && t.TransactionType == "Earning" && t.Timestamp.Month == DateTime.Now.Month)
                        .Sum(t => (decimal?)t.Amount) ?? 0m
                })
                .OrderByDescending(u => u.CurrentBalance)
                .ToListAsync();

            return View(payoutsData);
        }

        // Action for when the Admin actually pays the driver (e.g., via Bank Transfer)
        // and wants to clear/deduct the wallet balance.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SettleBalance(string userId, decimal amount, string reference)
        {
            if (amount <= 0)
            {
                TempData["Error"] = "Invalid payout amount.";
                return RedirectToAction("DriverPayouts");
            }

            // Deduct from wallet (Amount is negative because it's a payout/withdrawal)
            await db.AdjustWalletBalance(userId, -amount, $"Admin Payout: {reference}", "Withdrawal");
            await db.SaveChangesAsync();

            TempData["Success"] = "Driver balance settled and transaction logged.";
            return RedirectToAction("DriverPayouts");
        }
        public async Task<ActionResult> SystemTransactions()
        {
            var logs = await db.WalletTransactions
                .Include(t => t.User)
                .OrderByDescending(t => t.Timestamp)
                .Take(500)
                .Select(t => new TransactionViewModel
                {
                    Id = t.Id,
                    UserEmail = t.User != null ? t.User.Email : "System",
                    // Concatenate Name and Surname here
                    FullName = t.User != null ? t.User.Name + " " + t.User.Surname : "System Account",
                    Amount = t.Amount,
                    Type = t.TransactionType,
                    Timestamp = t.Timestamp,
                    Description = t.Description
                })
                .ToListAsync();

            return View(logs);
        }
        public async Task<ActionResult> Archives(string search, int? page)
        {
            // 1. Fetch GLOBAL stats first, before any filtering.
            var allShipmentsQuery = db.Shipments.AsQueryable();
            ViewBag.TotalRevenue = await allShipmentsQuery
                .Where(s => s.Status == AppConstants.ShipmentStatuses.Delivered)
                .SumAsync(s => (decimal?)s.Price) ?? 0m;
            int totalVolume = await allShipmentsQuery.CountAsync();
            int deliveredCount = await allShipmentsQuery.CountAsync(s => s.Status == AppConstants.ShipmentStatuses.Delivered);
            ViewBag.SuccessRate = totalVolume > 0 ? (double)deliveredCount / totalVolume * 100 : 0;
            ViewBag.TotalVolume = totalVolume;

            // 2. Filter the query based on the search string
            var pagedQuery = db.Shipments.OrderByDescending(s => s.CreatedDate).AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                pagedQuery = pagedQuery.Where(s =>
                    s.TrackingNumber.Contains(search) ||
                    s.ReceiverName.Contains(search) ||
                    s.ReceiverCity.Contains(search) ||
                    s.Status.Contains(search)
                );
            }

            // 3. Paginate the filtered results
            int pageNumber = page ?? 1;
            int pageSize = 25; // Show 25 records per page
            var pagedShipments = await pagedQuery.ToPagedListAsync(pageNumber, pageSize);

            ViewBag.CurrentSearch = search; // Pass search term back to the view

            return View(pagedShipments);
        }

        public async Task<ActionResult> AuditLogs(string search, int? page)
        {
            // 1. Create the base query with includes. Do NOT call ToList() yet.
            var logsQuery = db.StatusHistories
                .Include(h => h.Shipment)
                .Include(h => h.UpdatedBy)
                .OrderByDescending(h => h.Timestamp)
                .AsQueryable();

            // 2. Filter the query if a search term is provided
            if (!string.IsNullOrEmpty(search))
            {
                logsQuery = logsQuery.Where(h =>
                    h.Status.Contains(search) ||
                    h.Notes.Contains(search) ||
                    h.Shipment.TrackingNumber.Contains(search) ||
                    (h.UpdatedBy.Name + " " + h.UpdatedBy.Surname).Contains(search)
                );
            }

            // 3. Paginate the results
            int pageNumber = page ?? 1;
            int pageSize = 50; // Show 50 logs per page
            var pagedLogs = await logsQuery.ToPagedListAsync(pageNumber, pageSize);

            // 4. Pass the search term back to the view so the search box doesn't clear
            ViewBag.CurrentSearch = search;

            return View(pagedLogs);
        }
        public async Task<ActionResult> Details(int id)
        {
            var shipment = await db.Shipments
                .Include(s => s.AssignedDriver)
                .Include(s => s.StatusHistories)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null) return HttpNotFound();

            // 1. Fetch Dynamic Commission Rate
            var commissionSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "DriverCommissionRate");
            string rawValue = (commissionSetting?.Value ?? "0.70").Replace(",", ".").Replace("%", "").Trim();
            if (!decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal commissionRate))
            {
                commissionRate = 0.70m;
            }
            ViewBag.CommissionRate = commissionRate;

            // 2. Fetch Drivers for Dropdown
            var driverRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == AppConstants.Roles.Driver);
            ViewBag.Drivers = await db.Users.Where(u => u.Roles.Any(r => r.RoleId == driverRole.Id)).ToListAsync();

            return View(shipment);
        }

        // GET: Admin/Settings
        public async Task<ActionResult> Settings()
        {
            var settings = await db.SystemSettings.OrderBy(s => s.Key).ToListAsync();

            if (!settings.Any())
            {
                db.SystemSettings.AddRange(new List<SystemSetting> {
            new SystemSetting { Key = "BasePriceFee", Value = "40.00" },
            new SystemSetting { Key = "PerKgRateFee", Value = "12.00" },
            new SystemSetting { Key = "VatPercentage", Value = "15" },
            new SystemSetting { Key = "CollectionCommissionRate", Value = "0.30" }, // 30% for Pickup
            new SystemSetting { Key = "DeliveryCommissionRate", Value = "0.40" },   // 40% for Delivery
            new SystemSetting { Key = "SystemMaintenanceMode", Value = "OFF" }
        });
                await db.SaveChangesAsync();
                settings = await db.SystemSettings.ToListAsync();
            }

            return View(settings);
        }
        // POST: Admin/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Settings(List<SystemSetting> settings)
        {
            if (ModelState.IsValid)
            {
                foreach (var item in settings)
                {
                    var dbSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == item.Key);
                    if (dbSetting != null)
                    {
                        dbSetting.Value = item.Value;
                        db.Entry(dbSetting).State = EntityState.Modified;
                    }
                }
                await db.SaveChangesAsync();
                TempData["Success"] = "Global system configurations updated.";
                return RedirectToAction("Settings");
            }
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateStatus(int id, string newStatus, string notes, string assignedDriverId)
        {
            var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id);
            if (shipment == null) return HttpNotFound();

            string oldStatus = shipment.Status;
            shipment.Status = newStatus;

            // NEW: If a driver was selected in the dropdown, assign them here
            if (!string.IsNullOrEmpty(assignedDriverId))
            {
                shipment.AssignedDriverId = assignedDriverId;
            }

            // Add to history
            db.StatusHistories.Add(new StatusHistory
            {
                ShipmentId = id,
                Status = newStatus,
                Timestamp = DateTime.UtcNow,
                Notes = notes ?? $"Status updated by Admin ({User.Identity.Name})",
                UpdatedById = User.Identity.GetUserId()
            });

            await db.SaveChangesAsync();

            // TRIGGER EMAIL
            if (newStatus != oldStatus)
            {
                try
                {
                    await _emailService.SendStatusUpdateEmailAsync(shipment, shipment.SenderEmail, newStatus);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Status Email Failed: " + ex.Message);
                }
            }

            TempData["Success"] = $"Shipment {shipment.TrackingNumber} updated/assigned successfully.";
            return RedirectToAction("Index"); // Redirect to Index so the queue clears
        }


        // ==========================================
        // 5. SUPPORT & CUSTOMER RELATIONS (Admin Led)
        // ==========================================

        // List all incoming tickets
        public async Task<ActionResult> SupportTickets(string filter = "Open")
        {
            var query = db.SupportTickets
                .Include(t => t.Customer)
                .Include(t => t.RelatedShipment)
                .AsQueryable();

            if (filter == "Open")
                query = query.Where(t => t.Status == "Open" || t.Status == "In Progress");
            else if (filter == "Resolved")
                query = query.Where(t => t.Status == "Resolved");

            var tickets = await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
            ViewBag.CurrentFilter = filter;
            return View(tickets);
        }

        // View a ticket conversation and handle it
        public async Task<ActionResult> TicketDetails(int id)
        {
            var ticket = await db.SupportTickets
                .Include(t => t.Customer)
                .Include(t => t.RelatedShipment)
                .Include(t => t.Replies.Select(r => r.Author))
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return HttpNotFound();
            return View(ticket);
        }

        // Admin replies to a ticket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> TicketReply(int ticketId, string message, string newStatus)
        {
            var ticket = await db.SupportTickets.FindAsync(ticketId);
            if (ticket == null) return HttpNotFound();

            if (!string.IsNullOrWhiteSpace(message))
            {
                var reply = new TicketReply
                {
                    SupportTicketId = ticketId,
                    AuthorId = User.Identity.GetUserId(),
                    Message = message,
                    CreatedDate = DateTime.Now
                };
                db.TicketReplies.Add(reply);

                // Update Ticket Metadata
                ticket.Status = newStatus;
                ticket.LastUpdatedDate = DateTime.Now;
                ticket.AssignedToStaffId = User.Identity.GetUserId();

                await db.SaveChangesAsync();
                TempData["Success"] = "Reply sent and status updated.";
            }

            return RedirectToAction("TicketDetails", new { id = ticketId });
        }

        // View Customer Feedback/Ratings
        public async Task<ActionResult> CustomerFeedback()
        {
            var ratings = await db.ShipmentRatings
                .Include(r => r.Customer)
                .Include(r => r.Shipment)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(ratings);
        }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<ActionResult> AdminCancel(int id, string reason)
            {
                var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id);
                if (shipment == null) return HttpNotFound();

                // ... (Your existing refund logic) ...
                if (!string.IsNullOrEmpty(shipment.CustomerId))
                {
                    await db.AdjustWalletBalance(shipment.CustomerId, shipment.Price, $"Admin Void Refund: {shipment.TrackingNumber}", "Refund");
                }

                shipment.Status = AppConstants.ShipmentStatuses.Cancelled;

                // Add History
                db.StatusHistories.Add(new StatusHistory { /* ... your existing history logic ... */ });

                await db.SaveChangesAsync();

                // ==========================================
                // NOTIFY CUSTOMER OF CANCELLATION
                // ==========================================
                try
                {
                    await _emailService.SendStatusUpdateEmailAsync(shipment, shipment.SenderEmail, AppConstants.ShipmentStatuses.Cancelled);
                }
                catch { /* Log error */ }

                TempData["Success"] = $"Shipment {shipment.TrackingNumber} voided and customer notified.";
                return RedirectToAction("Index");
            } 
        


        protected override void Dispose(bool disposing)
        {
            if (disposing) { db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}