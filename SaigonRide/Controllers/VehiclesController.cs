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
    [Authorize(Roles = "Admin")]
   
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
            return DeleteVehicle(id);
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

            if (status != vehicle.Status && HasUnfinishedRental(vehicle.Id))
            {
                TempData["Error"] = "Cannot update this vehicle because it is currently rented by a user.";
                return RedirectToAction("Tracking");
            }

            var deltas = BuildInventoryDeltas(vehicle.StationId, vehicle.Status, vehicle.StationId, status);
            ValidateInventoryDeltas(deltas);
            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Unable to update status.";
                return RedirectToAction("Tracking");
            }

            vehicle.Status = status;
            ApplyInventoryDeltas(deltas);
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
            ViewBag.Quantity = 1;
            return View();
        }

        // POST: Vehicles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,VehicleCode,VehicleCategoryId,StationId,Status")] Vehicle vehicle, int quantity = 1)
        {
            if (quantity < 1)
            {
                ModelState.AddModelError("quantity", "Quantity must be at least 1.");
                quantity = 1;
            }
            else if (quantity > 100)
            {
                ModelState.AddModelError("quantity", "You can create up to 100 vehicles at a time.");
                quantity = 100;
            }

            ViewBag.Quantity = quantity;

            var vehicleCode = (vehicle.VehicleCode ?? string.Empty).Trim();
            vehicle.VehicleCode = vehicleCode;

            if (string.IsNullOrWhiteSpace(vehicleCode))
            {
                ModelState.AddModelError("VehicleCode", "Vehicle code is required.");
            }

            var vehicleCodes = BuildVehicleCodes(vehicleCode, quantity);

            if (vehicleCodes.Any(code => db.Vehicles.Any(v => v.VehicleCode == code)))
            {
                ModelState.AddModelError("VehicleCode", "One or more vehicle codes already exist.");
            }

            var station = db.Stations.Find(vehicle.StationId);
            if (station == null)
            {
                ModelState.AddModelError("StationId", "Station is required.");
            }
            else if (CountsAsStationInventory(vehicle.Status) && station.CurrentInventory + quantity > station.Capacity)
            {
                ModelState.AddModelError("StationId", "This station does not have enough capacity for that many vehicles.");
            }

            if (ModelState.IsValid)
            {
                foreach (var code in vehicleCodes)
                {
                    db.Vehicles.Add(new Vehicle
                    {
                        VehicleCode = code,
                        VehicleCategoryId = vehicle.VehicleCategoryId,
                        StationId = vehicle.StationId,
                        Status = vehicle.Status
                    });
                }

                if (CountsAsStationInventory(vehicle.Status))
                {
                    station.CurrentInventory += quantity;
                    db.Entry(station).State = EntityState.Modified;
                }

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
            var existing = db.Vehicles.AsNoTracking().FirstOrDefault(v => v.Id == vehicle.Id);
            if (existing == null)
            {
                return HttpNotFound();
            }

            if (db.Vehicles.Any(v => v.Id != vehicle.Id && v.VehicleCode == vehicle.VehicleCode))
            {
                ModelState.AddModelError("VehicleCode", "Vehicle code already exists.");
            }

            if (HasUnfinishedRental(vehicle.Id) && HasVehicleChanges(existing, vehicle))
            {
                ModelState.AddModelError("", "Cannot update this vehicle because it is currently rented by a user.");
            }

            var deltas = BuildInventoryDeltas(existing.StationId, existing.Status, vehicle.StationId, vehicle.Status);
            ValidateInventoryDeltas(deltas);

            if (ModelState.IsValid)
            {
                db.Entry(vehicle).State = EntityState.Modified;
                ApplyInventoryDeltas(deltas);
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
            return DeleteVehicle(id);
        }

        private ActionResult DeleteVehicle(int id)
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

            if (db.Rentals.Any(r => r.VehicleId == id))
            {
                TempData["Error"] = "Cannot delete this vehicle because it has rental history.";
                return RedirectToAction("Index");
            }

            if (CountsAsStationInventory(vehicle.Status))
            {
                var station = db.Stations.Find(vehicle.StationId);
                if (station != null && station.CurrentInventory > 0)
                {
                    station.CurrentInventory -= 1;
                    db.Entry(station).State = EntityState.Modified;
                }
            }

            db.Vehicles.Remove(vehicle);
            db.SaveChanges();
            TempData["Info"] = "Vehicle deleted.";
            return RedirectToAction("Index");
        }

        private static bool CountsAsStationInventory(VehicleStatus status)
        {
            return status != VehicleStatus.InTransit;
        }

        private bool HasUnfinishedRental(int vehicleId)
        {
            return db.Rentals.Any(r =>
                r.VehicleId == vehicleId &&
                r.Status != RentalStatus.Cancelled &&
                (!r.EndTime.HasValue ||
                 !db.Payments.Any(p => p.RentalId == r.Id && p.Status == PaymentStatus.Success)));
        }

        private static bool HasVehicleChanges(Vehicle existing, Vehicle updated)
        {
            return existing.VehicleCode != updated.VehicleCode ||
                   existing.VehicleCategoryId != updated.VehicleCategoryId ||
                   existing.StationId != updated.StationId ||
                   existing.Status != updated.Status;
        }

        private static List<string> BuildVehicleCodes(string vehicleCode, int quantity)
        {
            if (quantity <= 1)
            {
                return new List<string> { vehicleCode };
            }

            return Enumerable.Range(1, quantity)
                .Select(number => $"{vehicleCode}-{number:000}")
                .ToList();
        }

        private Dictionary<int, int> BuildInventoryDeltas(int oldStationId, VehicleStatus oldStatus, int newStationId, VehicleStatus newStatus)
        {
            var deltas = new Dictionary<int, int>();

            if (CountsAsStationInventory(oldStatus))
            {
                AddDelta(deltas, oldStationId, -1);
            }

            if (CountsAsStationInventory(newStatus))
            {
                AddDelta(deltas, newStationId, 1);
            }

            return deltas.Where(d => d.Value != 0).ToDictionary(d => d.Key, d => d.Value);
        }

        private static void AddDelta(Dictionary<int, int> deltas, int stationId, int delta)
        {
            if (!deltas.ContainsKey(stationId))
            {
                deltas[stationId] = 0;
            }

            deltas[stationId] += delta;
        }

        private void ValidateInventoryDeltas(Dictionary<int, int> deltas)
        {
            foreach (var delta in deltas)
            {
                var station = db.Stations.Find(delta.Key);
                if (station == null)
                {
                    ModelState.AddModelError("StationId", "Station is required.");
                    continue;
                }

                var nextInventory = station.CurrentInventory + delta.Value;
                if (nextInventory < 0)
                {
                    ModelState.AddModelError("StationId", "Station inventory cannot become negative.");
                }

                if (nextInventory > station.Capacity)
                {
                    ModelState.AddModelError("StationId", "This station does not have enough capacity.");
                }
            }
        }

        private void ApplyInventoryDeltas(Dictionary<int, int> deltas)
        {
            foreach (var delta in deltas)
            {
                var station = db.Stations.Find(delta.Key);
                if (station == null)
                {
                    continue;
                }

                station.CurrentInventory += delta.Value;
                db.Entry(station).State = EntityState.Modified;
            }
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
