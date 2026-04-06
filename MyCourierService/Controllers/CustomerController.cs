using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MyCourierSA.Constants;
using MyCourierSA.Models;
using MyCourierSA.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyCourierSA.Controllers
{
    [Authorize(Roles = AppConstants.Roles.Customer)]
    public class CustomerController : Controller
    {
        // Ideally, inject this via constructor (Dependency Injection)
        private readonly ApplicationDbContext db;
        private ApplicationUserManager _userManager;
        private readonly PricingService _pricingService; // <--- ADD THIS LINE
        private readonly ShipmentEmailService _emailService;
        public ApplicationUserManager UserManager => _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();

        // Helper property to keep code DRY
        private string CurrentUserId => User.Identity.GetUserId();

        public CustomerController()
        {
            db = new ApplicationDbContext();
            _pricingService = new PricingService(db);
            _emailService = new ShipmentEmailService();
        }

        // ==========================================
        // 1. DASHBOARD & STATS
        // ==========================================
        public async Task<ActionResult> Index()
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            ViewBag.UserName = user != null ? $"{user.Name} {user.Surname}" : "Customer";
            ViewBag.WalletBalance = user?.WalletBalance ?? 0;

            var shipments = await db.Shipments
                .AsNoTracking() // Use AsNoTracking for read-only queries to boost performance
                .Where(s => s.CustomerId == CurrentUserId)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            ViewBag.Total = shipments.Count;
            ViewBag.Pending = shipments.Count(s => s.Status == AppConstants.ShipmentStatuses.Pending || s.Status == AppConstants.ShipmentStatuses.Approved);
            ViewBag.InTransit = shipments.Count(s => s.Status == AppConstants.ShipmentStatuses.InTransit || s.Status == AppConstants.ShipmentStatuses.AssignedForDelivery);
            ViewBag.Delivered = shipments.Count(s => s.Status == AppConstants.ShipmentStatuses.Delivered);

            return View(shipments);
        }
        // ==========================================
        // 2. WALLET & TOP-UP
        // ==========================================
        [HttpGet]
        public async Task<ActionResult> Wallet()
        {
            var userId = User.Identity.GetUserId();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            ViewBag.TransactionHistory = await db.WalletTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .Take(15).ToListAsync();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> TopUp(decimal amount)
        {
            if (amount <= 0) return RedirectToAction("Wallet");

            // Use the atomic method we created in Step 1
            bool success = await db.AdjustWalletBalance(CurrentUserId, amount, "Wallet Top-up (Manual)", "Deposit");

            if (success) TempData["Success"] = $"R{amount:F2} added to wallet.";
            else TempData["Error"] = "Top-up failed.";

            return RedirectToAction("Wallet");
        }

        [HttpGet] 
          public ActionResult CreateShipment()
        {
            // Make sure this name matches your .cshtml file exactly
            return View(new CreateShipmentViewModel
            {
                ParcelTypes = GetParcelTypes(),
                DeliveryOptions = GetDeliveryOptions(),
                PickupDateTime = DateTime.Now.AddHours(2)
            });
        }
        // ==========================================
        // 3. BOOKING LOGIC
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateShipment(CreateShipmentViewModel model, IEnumerable<HttpPostedFileBase> ParcelFiles)
        {
            // 1. Repopulate dropdown lists
            model.ParcelTypes = GetParcelTypes();
            model.DeliveryOptions = GetDeliveryOptions();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 2. Use Pricing Service
            decimal price = await _pricingService.CalculatePriceAsync(model.ParcelWeight, model.ParcelType, model.DeliveryOption);

            // 3. ATOMIC PAYMENT
            bool paymentSuccessful = await db.AdjustWalletBalance(
                CurrentUserId,
                -price,
                $"Shipment Booking Fee (Pending)",
                AppConstants.TransactionTypes.Payment);

            if (!paymentSuccessful)
            {
                TempData["Error"] = $"Insufficient funds. This shipment costs R{price:F2}. Please top up your wallet.";
                return RedirectToAction("Wallet");
            }

            // 4. Create the Shipment Record
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);

                    var shipment = new Shipment
                    {
                        CustomerId = CurrentUserId,
                        SenderName = model.SenderName,
                        SenderAddress = model.SenderAddress,
                        SenderCity = model.SenderCity,
                        SenderProvince = model.SenderProvince,
                        SenderEmail = model.SenderEmail ?? user.Email,
                        SenderPhone = model.SenderPhone ?? user.PhoneNumber,

                        ReceiverName = model.ReceiverName,
                        ReceiverAddress = model.ReceiverAddress,
                        ReceiverCity = model.ReceiverCity,
                        ReceiverProvince = model.ReceiverProvince,
                        ReceiverEmail = model.ReceiverEmail,
                        ReceiverPhone = model.ReceiverPhone,

                        PickupAddress = model.PickupAddress,
                        PickupDateTime = model.PickupDateTime,
                        ParcelType = model.ParcelType,
                        ParcelWeight = model.ParcelWeight,
                        IsFragile = model.IsFragile,
                        DeliveryOption = model.DeliveryOption,

                        TrackingNumber = "MC" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        Price = price,
                        Status = AppConstants.ShipmentStatuses.Pending,
                        CreatedDate = DateTime.UtcNow
                    };

                    db.Shipments.Add(shipment);

                    // 5. Add Status History
                    db.StatusHistories.Add(new StatusHistory
                    {
                        Shipment = shipment,
                        Status = AppConstants.ShipmentStatuses.Pending,
                        Timestamp = DateTime.UtcNow,
                        Notes = "Shipment booked and paid via Customer Portal."
                    });

                    // 6. Handle File Uploads
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

                    await db.SaveChangesAsync();
                    transaction.Commit();

                    // ==========================================
                    // NEW: SEND CONFIRMATION EMAIL
                    // ==========================================
                    try
                    {
                        // We send it to the user's registered email
                        await _emailService.SendShipmentCreatedEmailAsync(shipment, user.Email);
                    }
                    catch (Exception emailEx)
                    {
                        // We log this but don't stop the user from seeing the success page
                        System.Diagnostics.Debug.WriteLine("Email failed: " + emailEx.Message);
                    }
                    // ==========================================

                    TempData["Success"] = $"Shipment {shipment.TrackingNumber} created and paid successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    // 7. CRITICAL: REFUND
                    await db.AdjustWalletBalance(
                        CurrentUserId,
                        price,
                        $"System Refund: Failed Booking Creation",
                        "Refund");

                    ModelState.AddModelError("", "A system error occurred. Your wallet has been refunded. Error: " + ex.Message);
                }
            }

            return View(model);
        }

    
        // ==========================================
        // 4. JOURNEY TRACKING & DETAILS
        // ==========================================
        [HttpGet]
        public async Task<ActionResult> Details(int id)
        {
            var userId = User.Identity.GetUserId();
            // SECURITY: Ensure user owns this parcel
            var shipment = await db.Shipments
                .Include(s => s.AssignedDriver)
                .Include(s => s.StatusHistories)
                .Include(s => s.ShipmentFiles)
                .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == userId);

            if (shipment == null) return HttpNotFound();
            return View(shipment);
        }

        public async Task<ActionResult> History()
        {
            var userId = User.Identity.GetUserId();
            var history = await db.Shipments
                .Where(s => s.CustomerId == userId)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            return View(history);
        }

        // Inside CustomerController.cs

        [HttpGet]
        public async Task<ActionResult> GetShipmentImage(int fileId)
        {
            var currentUserId = User.Identity.GetUserId();

            // SECURITY FIX: Find the file AND ensure it belongs to the logged-in customer
            var file = await db.ShipmentFiles
                .Include(f => f.Shipment)
                .FirstOrDefaultAsync(f => f.Id == fileId && f.Shipment.CustomerId == currentUserId);

            if (file == null)
            {
                // This prevents Customer A from seeing Customer B's files
                return HttpNotFound("You do not have permission to view this file or it does not exist.");
            }

            return File(file.Content, file.ContentType);
        }

        // ==========================================
        // 5. SUPPORT HUB & TICKETING
        // ==========================================

        [HttpGet]
        public async Task<ActionResult> Support()
        {
            var userId = CurrentUserId;

            // 1. Get recent shipments for the "File a Claim" dropdown
            ViewBag.MyShipments = await db.Shipments
                .Where(s => s.CustomerId == userId)
                .OrderByDescending(s => s.CreatedDate)
                .Take(10).ToListAsync();

            // 2. Get my active tickets
            var myTickets = await db.SupportTickets
                .Where(t => t.CustomerId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            return View(myTickets);
        }

        // Handles both General Inquiries and Shipment Claims
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SubmitTicket(string subject, string message, int? shipmentId)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Subject and Message are required.";
                return RedirectToAction("Support");
            }

            // CREATE THE TICKET WITH ALL REQUIRED FIELDS
            var ticket = new SupportTicket
            {
                CustomerId = CurrentUserId,
                Subject = subject,
                InitialMessage = message, // Ensure this matches your property name (InitialMessage or Message)
                ShipmentId = shipmentId,
                Status = "Open",
                Priority = "Normal",        // <--- ADD THIS (Required field)
                CreatedDate = DateTime.Now,
                LastUpdatedDate = DateTime.Now // <--- ADD THIS (Required field)
            };

            try
            {
                db.SupportTickets.Add(ticket);
                await db.SaveChangesAsync();
                TempData["Success"] = "Support ticket created. Admin will review it shortly.";
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // This helper block will print the EXACT field that is failing to your Debug window
                foreach (var failure in ex.EntityValidationErrors)
                {
                    foreach (var error in failure.ValidationErrors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Property: {error.PropertyName} Error: {error.ErrorMessage}");
                    }
                }
                TempData["Error"] = "Critical database error. Please contact support.";
            }

            return RedirectToAction("Support");
        }
        // View a specific ticket conversation
        [HttpGet]
        public async Task<ActionResult> ViewTicket(int id)
        {
            var ticket = await db.SupportTickets
                .Include(t => t.Replies.Select(r => r.Author))
                .Include(t => t.RelatedShipment)
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == CurrentUserId);

            if (ticket == null) return HttpNotFound();
            return View(ticket);
        }

        // Post a reply to an existing ticket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostReply(int ticketId, string message)
        {
            var ticket = await db.SupportTickets.AnyAsync(t => t.Id == ticketId && t.CustomerId == CurrentUserId);
            if (!ticket) return HttpNotFound();

            var reply = new TicketReply
            {
                SupportTicketId = ticketId,
                AuthorId = CurrentUserId,
                Message = message,
                CreatedDate = DateTime.Now
            };

            db.TicketReplies.Add(reply);
            await db.SaveChangesAsync();

            return RedirectToAction("ViewTicket", new { id = ticketId });
        }

        // 1. THIS SHOWS THE RATING PAGE (The GET request)
        [HttpGet]
        public async Task<ActionResult> SubmitRating(int id)
        {
            // SECURITY: Find the shipment AND ensure it belongs to the logged-in customer
            var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == CurrentUserId);

            // If shipment doesn't exist or isn't delivered yet, don't allow rating
            if (shipment == null || shipment.Status != AppConstants.ShipmentStatuses.Delivered)
            {
                return HttpNotFound();
            }

            // Check if they already rated this shipment (to prevent double ratings)
            var alreadyRated = await db.ShipmentRatings.AnyAsync(r => r.ShipmentId == id);
            if (alreadyRated)
            {
                TempData["Error"] = "You have already provided feedback for this delivery.";
                return RedirectToAction("Details", new { id = id });
            }

            return View(shipment);
        }

        // 2. THIS SAVES THE RATING DATA (The POST request)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SubmitRating(int shipmentId, int stars, string comments)
        {
            // Verify ownership and status again for security
            var shipment = await db.Shipments.AnyAsync(s => s.Id == shipmentId && s.CustomerId == CurrentUserId && s.Status == AppConstants.ShipmentStatuses.Delivered);

            if (!shipment) return HttpNotFound();

            // Create the new Rating record
            var rating = new ShipmentRating
            {
                ShipmentId = shipmentId,
                CustomerId = CurrentUserId,
                Stars = stars,
                Comments = comments,
                CreatedDate = DateTime.Now
            };

            db.ShipmentRatings.Add(rating);
            await db.SaveChangesAsync();

            TempData["Success"] = "Thank you! Your feedback has been recorded.";

            // Send them back to the shipment details page
            return RedirectToAction("Details", new { id = shipmentId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelShipment(int id)
        {
            var userId = CurrentUserId;
            var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == userId);

            if (shipment == null || shipment.Status != AppConstants.ShipmentStatuses.Pending)
            {
                TempData["Error"] = "Cannot cancel at this stage.";
                return RedirectToAction("Index");
            }

            // Refund and Update Status
            bool refunded = await db.AdjustWalletBalance(userId, shipment.Price, $"Refund for Cancelled: {shipment.TrackingNumber}", "Refund");

            if (refunded)
            {
                shipment.Status = AppConstants.ShipmentStatuses.Cancelled;
                await db.SaveChangesAsync();
                TempData["Success"] = "Cancelled and Refunded.";
            }

            return RedirectToAction("Index");
        }
        // ==========================================
        // HELPERS
        // ==========================================


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