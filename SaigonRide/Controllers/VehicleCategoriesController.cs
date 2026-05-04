using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SaigonRide.Models;
using System.Data.Entity.Migrations;
using System.Data.Entity.Infrastructure;

namespace SaigonRide.Controllers
{
    public class VehicleCategoriesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: VehicleCategories
        public ActionResult Index()
        {
            return View(db.VehicleCategories.ToList());
        }

        // GET: VehicleCategories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VehicleCategory vehicleCategory = db.VehicleCategories.Find(id);
            if (vehicleCategory == null)
            {
                return HttpNotFound();
            }
            return View(vehicleCategory);
        }

        // GET: VehicleCategories/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: VehicleCategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,PricePerMinute")] VehicleCategory vehicleCategory)
        {
            if (ModelState.IsValid)
            {
                db.VehicleCategories.Add(vehicleCategory);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(vehicleCategory);
        }

        // GET: VehicleCategories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VehicleCategory vehicleCategory = db.VehicleCategories.Find(id);
            if (vehicleCategory == null)
            {
                return HttpNotFound();
            }
            return View(vehicleCategory);
        }

        // POST: VehicleCategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,PricePerMinute")] VehicleCategory vehicleCategory)
        {
            if (ModelState.IsValid)
            {
                db.Entry(vehicleCategory).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(vehicleCategory);
        }

        // GET: VehicleCategories/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VehicleCategory vehicleCategory = db.VehicleCategories.Find(id);
            if (vehicleCategory == null)
            {
                return HttpNotFound();
            }
            return View(vehicleCategory);
        }

        // POST: VehicleCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            VehicleCategory vehicleCategory = db.VehicleCategories.Find(id);
            db.VehicleCategories.Remove(vehicleCategory);
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

        static VehicleCategoriesController()
        {
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<
                    SaigonRide.Models.ApplicationDbContext,
                    SaigonRide.Migrations.Configuration>());

        }
    }
}
