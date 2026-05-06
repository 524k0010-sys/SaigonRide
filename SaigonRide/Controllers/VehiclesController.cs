using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SaigonRide.Models;

namespace SaigonRide.Controllers
{
    [Authorize]
   
    public class VehiclesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Vehicles
        public ActionResult Index()
        {
            var vehicles = db.Vehicles.Include(v => v.Station).Include(v => v.VehicleCategory);
            return View(vehicles.ToList());
        }

        // POST: Vehicles/DeleteFromIndex/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteFromIndex(int id)
        {
            var vehicle = db.Vehicles.Find(id);
            if (vehicle == null)
            {
                return HttpNotFound();
            }

            if (vehicle.Status == VehicleStatus.InTransit)
            {
                TempData["Error"] = "Cannot delete a vehicle that is currently in transit.";
                return RedirectToAction("Index");
            }

            // remove dependent payments and rentals to allow deletion
            var rentals = db.Rentals.Where(r => r.VehicleId == id).ToList();
            if (rentals.Any())
            {
                // remove payments linked to these rentals first
                var rentalIds = rentals.Select(r => r.Id).ToList();
                var payments = db.Payments.Where(p => rentalIds.Contains(p.RentalId)).ToList();
                if (payments.Any()) db.Payments.RemoveRange(payments);

                db.Rentals.RemoveRange(rentals);
            }

            db.Vehicles.Remove(vehicle);
            db.SaveChanges();

            TempData["Info"] = "Vehicle deleted.";
            return RedirectToAction("Index");
        }

        // GET: Vehicles/Tracking
        public ActionResult Tracking(string status, int? stationId)
        {
            var query = db.Vehicles.Include(v => v.Station).Include(v => v.VehicleCategory).AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<VehicleStatus>(status, true, out var vs))
                {
                    query = query.Where(v => v.Status == vs);
                }
            }

            if (stationId.HasValue)
            {
                query = query.Where(v => v.StationId == stationId.Value);
            }

            var vehicles = query.ToList();

            // last activity (last rental end time or start time)
            var lastMap = db.Rentals
                .GroupBy(r => r.VehicleId)
                .Select(g => new { VehicleId = g.Key, Last = g.Max(r => (DateTime?) (r.EndTime ?? r.StartTime)) })
                .ToDictionary(x => x.VehicleId, x => x.Last);

            ViewBag.LastActivity = lastMap;
            ViewBag.Stations = new SelectList(db.Stations.OrderBy(s => s.Name).ToList(), "Id", "Name");
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedStation = stationId;

            return View(vehicles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetStatus(int id, VehicleStatus status)
        {
            var vehicle = db.Vehicles.Find(id);
            if (vehicle == null)
            {
                return HttpNotFound();
            }

            vehicle.Status = status;
            db.Entry(vehicle).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Tracking");
        }

        // GET: Vehicles/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vehicle vehicle = db.Vehicles.Find(id);
            if (vehicle == null)
            {
                return HttpNotFound();
            }
            return View(vehicle);
        }

        // GET: Vehicles/Create
        public ActionResult Create()
        {
            ViewBag.StationId = new SelectList(db.Stations, "Id", "Name");
            ViewBag.VehicleCategoryId = new SelectList(db.VehicleCategories, "Id", "Name");
            return View();
        }

        // POST: Vehicles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,VehicleCode,VehicleCategoryId,StationId,Status")] Vehicle vehicle)
        {
            if (db.Vehicles.Any(v => v.VehicleCode == vehicle.VehicleCode))
            {
                ModelState.AddModelError("VehicleCode", "Vehicle code already exists.");
            }
            if (ModelState.IsValid)
            {
                db.Vehicles.Add(vehicle);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.StationId = new SelectList(db.Stations, "Id", "Name", vehicle.StationId);
            ViewBag.VehicleCategoryId = new SelectList(db.VehicleCategories, "Id", "Name", vehicle.VehicleCategoryId);
            return View(vehicle);
        }

        // GET: Vehicles/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vehicle vehicle = db.Vehicles.Find(id);
            if (vehicle == null)
            {
                return HttpNotFound();
            }
            ViewBag.StationId = new SelectList(db.Stations, "Id", "Name", vehicle.StationId);
            ViewBag.VehicleCategoryId = new SelectList(db.VehicleCategories, "Id", "Name", vehicle.VehicleCategoryId);
            return View(vehicle);
        }

        // POST: Vehicles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,VehicleCode,VehicleCategoryId,StationId,Status")] Vehicle vehicle)
        {
            if (ModelState.IsValid)
            {
                db.Entry(vehicle).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.StationId = new SelectList(db.Stations, "Id", "Name", vehicle.StationId);
            ViewBag.VehicleCategoryId = new SelectList(db.VehicleCategories, "Id", "Name", vehicle.VehicleCategoryId);
            return View(vehicle);
        }

        // GET: Vehicles/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vehicle vehicle = db.Vehicles.Find(id);
            if (vehicle == null)
            {
                return HttpNotFound();
            }
            return View(vehicle);
        }

        // POST: Vehicles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Vehicle vehicle = db.Vehicles.Find(id);

            if (vehicle == null)
            {
                return HttpNotFound();
            }

            if (vehicle.Status == VehicleStatus.InTransit)
            {
                TempData["Error"] = "Cannot delete a vehicle that is currently in transit.";
                return RedirectToAction("Index");
            }

            // Prevent delete when there are related rentals/payments to avoid DB referential integrity errors
            var hasRentals = db.Rentals.Any(r => r.VehicleId == id);
            if (hasRentals)
            {
                TempData["Error"] = "Cannot delete this vehicle because it has rental history. Remove related rentals/payments first.";
                return RedirectToAction("Index");
            }

            db.Vehicles.Remove(vehicle);
            db.SaveChanges();
            return RedirectToAction("Index");
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
