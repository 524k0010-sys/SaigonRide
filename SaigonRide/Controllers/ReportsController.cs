using System.Web.Mvc;
using SaigonRide.Models;
using SaigonRide.Services;

namespace SaigonRide.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Inventory()
        {
            var service = new ReportService(db);
            var report = service.GetStationInventoryReport();
            return View(report);
        }
        public ActionResult Revenue()
        {
            var service = new ReportService(db);
            var report = service.GetRevenueByCategoryReport();
            return View(report);
        }

    }
}
