using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using SaigonRide.Models;
using SaigonRide.Services;

namespace SaigonRide.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [Authorize(Roles = "Admin")]
        public ActionResult Inventory()
        {
            var service = new ReportService(db);
            var report = service.GetStationInventoryReport();
            return View(report);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Revenue()
        {
            var service = new ReportService(db);
            var report = service.GetRevenueByCategoryReport();
            return View(report);
        }

        public ActionResult MyRevenue()
        {
            var userId = User.Identity.GetUserId();
            var rentals = db.Rentals
                .Include(r => r.Vehicle.VehicleCategory)
                .Include(r => r.StartStation)
                .Include(r => r.ReturnStation)
                .Where(r =>
                    r.UserId == userId &&
                    r.EndTime.HasValue &&
                    r.Status == RentalStatus.Completed)
                .OrderByDescending(r => r.EndTime)
                .ToList();

            ViewBag.TotalRentals = rentals.Count;
            ViewBag.TotalRevenue = rentals.Sum(r => r.TotalFare);

            return View(rentals);
        }
    }
}
