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
    public class StationsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Stations
        public ActionResult Index()
        {
            return View(db.Stations.ToList());
        }

        // GET: Stations/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Station station = db.Stations.Find(id);
            if (station == null)
            {
                return HttpNotFound();
            }
            return View(station);
        }

        // GET: Stations/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Stations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,Capacity,CurrentInventory")] Station station)
        {
            if (ModelState.IsValid)
            {
                db.Stations.Add(station);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(station);
        }

        // GET: Stations/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Station station = db.Stations.Find(id);
            if (station == null)
            {
                return HttpNotFound();
            }
            return View(station);
        }

        // POST: Stations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Capacity,CurrentInventory")] Station station)
        {
            if (ModelState.IsValid)
            {
                db.Entry(station).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(station);
        }

        // GET: Stations/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Station station = db.Stations.Find(id);
            if (station == null)
            {
                return HttpNotFound();
            }
            return View(station);
        }

        // POST: Stations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Station station = db.Stations.Find(id);
            db.Stations.Remove(station);
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
