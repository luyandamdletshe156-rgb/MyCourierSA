using MyCourierSA.Models;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Linq;

namespace MyCourierSA.Controllers
{
    [AllowAnonymous] // Open to everyone
    public class TrackingController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Show the search box
        public ActionResult Index()
        {
            return View();
        }

        // POST: Handle the search button
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                TempData["Error"] = "Please enter a tracking number.";
                return RedirectToAction("Index");
            }

            var shipment = await db.Shipments
                .Include(s => s.StatusHistories)
                .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber.Trim());

            if (shipment == null)
            {
                TempData["Error"] = "No shipment found with that number. Please verify and try again.";
                return RedirectToAction("Index");
            }

            // Return the specific 'Result' view you just created
            return View("Result", shipment);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}