using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using SaigonRide.Models;
using SaigonRide.Services;
using SaigonRide.ViewModels;

namespace SaigonRide.Controllers
{
    [Authorize]
    public class RentalsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();



        public ActionResult Available()
        {
            // hide vehicles that were returned very recently so they don't show up immediately
            // after a return/payment. Use a short cooldown window (2 minutes).
            var cutoff = DateTime.Now.AddMinutes(-2);

            var vehicles = db.Vehicles
                .Include(v => v.Station)
                .Include(v => v.VehicleCategory)
                .Where(v => v.Status == VehicleStatus.Ready &&
                            !db.Rentals.Any(r => r.VehicleId == v.Id && r.EndTime.HasValue && r.EndTime.Value >= cutoff))
                .ToList();

            return View(vehicles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StartTrip(int id, string returnTo = null)
        {
            var vehicle = db.Vehicles
                .Include(v => v.Station)
                .FirstOrDefault(v => v.Id == id);

            if (vehicle == null)
            {
                TempData["Error"] = "Vehicle not found.";
                return RedirectToAction("Available");
            }

            if (vehicle.Status != VehicleStatus.Ready)
            {
                TempData["Error"] = "This vehicle is not available.";
                return RedirectToAction("Available");
            }

            var station = db.Stations.Find(vehicle.StationId);

            var rental = new Rental
            {
                UserId = User.Identity.GetUserId(),
                VehicleId = vehicle.Id,
                StartStationId = vehicle.StationId,
                StartTime = DateTime.Now
            };

            vehicle.Status = VehicleStatus.InTransit;

            // ensure vehicle status update is tracked
            db.Entry(vehicle).State = EntityState.Modified;

            if (station != null && station.CurrentInventory > 0)
            {
                station.CurrentInventory -= 1;
                db.Entry(station).State = EntityState.Modified;
            }

            db.Rentals.Add(rental);
            db.SaveChanges();

            // If called via AJAX, return JSON so client can update UI immediately.
            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true, rentalId = rental.Id });
            }

            // If caller requested to be returned to Vehicles index or Available (rent) page, handle those
            if (!string.IsNullOrEmpty(returnTo))
            {
                if (returnTo.Equals("vehicles", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Info"] = $"Vehicle {vehicle.VehicleCode} started (InTransit).";
                    return RedirectToAction("Index", "Vehicles");
                }

                if (returnTo.Equals("available", StringComparison.OrdinalIgnoreCase) ||
                    returnTo.Equals("rent", StringComparison.OrdinalIgnoreCase) ||
                    returnTo.Equals("rentals", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Info"] = $"Vehicle {vehicle.VehicleCode} started (InTransit).";
                    return RedirectToAction("Available", "Rentals");
                }
            }

            // After starting a trip show an in-progress page. Customers can end trip; admins have admin return controls.
            return RedirectToAction("TripInProgress", new { id = rental.Id });
        }

        // GET: Rentals/TripInProgress/5
        public ActionResult TripInProgress(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Trip id is required.";
                return RedirectToAction("Available");
            }

            var rental = db.Rentals
                .Include(r => r.Vehicle.VehicleCategory)
                .Include(r => r.StartStation)
                .FirstOrDefault(r => r.Id == id.Value);

            if (rental == null)
            {
                return HttpNotFound();
            }

            // only owner or admin can view
            if (!User.IsInRole("Admin") && rental.UserId != User.Identity.GetUserId())
            {
                return new HttpUnauthorizedResult();
            }

            return View(rental);
        }

        public ActionResult EndTrip(int id)
        {
            var rental = db.Rentals
                .Include(r => r.Vehicle)
                .Include(r => r.StartStation)
                .FirstOrDefault(r => r.Id == id);

            if (rental == null)
            {
                return HttpNotFound();
            }

            var model = new EndTripViewModel
            {
                RentalId = rental.Id,
                VehicleCode = rental.Vehicle.VehicleCode,
                StartStationName = rental.StartStation.Name,
                StartTime = rental.StartTime,
                UserType = "Local",
                ReturnStations = GetStationOptions()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EndTrip(EndTripViewModel model)
        {
            var rental = db.Rentals
                .Include(r => r.Vehicle.VehicleCategory)
                .FirstOrDefault(r => r.Id == model.RentalId);

            var returnStation = db.Stations.Find(model.ReturnStationId);

            if (rental == null || returnStation == null)
            {
                return HttpNotFound();
            }

            if (returnStation.CurrentInventory >= returnStation.Capacity)
            {
                ModelState.AddModelError("ReturnStationId", "This station is full. Please choose another return station.");
            }

            if (!ModelState.IsValid)
            {
                model.ReturnStations = GetStationOptions();
                return View(model);
            }

            var pricingService = new PricingService();
            var endTime = DateTime.Now;
            var baseFare = pricingService.CalculateBaseFare(
                rental.StartTime,
                endTime,
                rental.Vehicle.VehicleCategory.PricePerMinute
            );

            var discount = pricingService.CalculateDiscount(baseFare, returnStation);
            var totalFare = pricingService.CalculateTotalFare(baseFare, discount);

            rental.EndTime = endTime;
            rental.ReturnStationId = returnStation.Id;
            rental.BaseFare = baseFare;
            rental.DiscountAmount = discount;
            rental.TotalFare = totalFare;

            db.SaveChanges();

            return RedirectToAction("Checkout", new { id = rental.Id, userType = model.UserType });
        }

        public ActionResult Checkout(int id, string userType)
        {
            var rental = db.Rentals
                .Include(r => r.Vehicle.VehicleCategory)
                .Include(r => r.ReturnStation)
                .FirstOrDefault(r => r.Id == id);

            if (rental == null)
            {
                return HttpNotFound();
            }

            var duration = (int)Math.Ceiling((rental.EndTime.Value - rental.StartTime).TotalMinutes);

            var model = new CheckoutViewModel
            {
                RentalId = rental.Id,
                VehicleCode = rental.Vehicle.VehicleCode,
                VehicleCategory = rental.Vehicle.VehicleCategory.Name,
                ReturnStationName = rental.ReturnStation.Name,
                UserType = string.IsNullOrEmpty(userType) ? "Local" : userType,
                DurationMinutes = duration,
                BaseFare = rental.BaseFare,
                DiscountAmount = rental.DiscountAmount,
                TotalFare = rental.TotalFare,
                PaymentMethods = GetPaymentOptions(userType)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPayment(CheckoutViewModel model)
        {
            // load rental together with its vehicle and return station to ensure updates are tracked
            var rental = db.Rentals
                .Include(r => r.Vehicle)
                .Include(r => r.ReturnStation)
                .FirstOrDefault(r => r.Id == model.RentalId);

            if (rental == null)
            {
                return HttpNotFound();
            }

            var allowedMethods = GetAllowedPaymentMethods(model.UserType);

            if (!allowedMethods.Contains(model.PaymentMethod))
            {
                ModelState.AddModelError("PaymentMethod", "This payment method is not allowed for this user type.");
            }

            if (!ModelState.IsValid)
            {
                model.PaymentMethods = GetPaymentOptions(model.UserType);
                return View("Checkout", model);
            }

            var payment = new Payment
            {
                RentalId = rental.Id,
                Method = model.PaymentMethod,
                Status = PaymentStatus.Success,
                Amount = rental.TotalFare,
                PaidAt = DateTime.Now
            };

            // update vehicle and station via tracked entities
            var vehicle = rental.Vehicle;
            var returnStation = rental.ReturnStation;

            if (vehicle != null)
            {
                vehicle.Status = VehicleStatus.Ready;

                if (returnStation != null)
                {
                    vehicle.StationId = returnStation.Id;

                    if (returnStation.CurrentInventory < returnStation.Capacity)
                    {
                        returnStation.CurrentInventory += 1;
                        db.Entry(returnStation).State = EntityState.Modified;
                    }
                }

                db.Entry(vehicle).State = EntityState.Modified;
            }

            db.Payments.Add(payment);
            db.SaveChanges();

            return RedirectToAction("Success", new { id = rental.Id });
        }

        public ActionResult Success(int id)
        {
            var payment = db.Payments
                .Include(p => p.Rental.Vehicle.VehicleCategory)
                .Include(p => p.Rental.ReturnStation)
                .FirstOrDefault(p => p.RentalId == id);

            if (payment == null)
            {
                return HttpNotFound();
            }

            return View(payment);
        }

        // GET: Rentals/MyRentals
        public ActionResult MyRentals()
        {
            var isAdmin = User.IsInRole("Admin");

            ViewBag.CurrentUserId = User.Identity.GetUserId();

            var rentals = db.Rentals
                .Include(r => r.Vehicle)
                .Include(r => r.StartStation)
                .Include(r => r.ReturnStation)
                .AsQueryable();

            if (!isAdmin)
            {
                var userId = User.Identity.GetUserId();
                rentals = rentals.Where(r => r.UserId == userId);
            }

            ViewBag.IsAdmin = isAdmin;

            return View(rentals.OrderByDescending(r => r.StartTime).ToList());
        }

        // GET: Rentals/ReturnVehicle/5
        public ActionResult ReturnVehicle(int id)
        {
            var rental = db.Rentals
                .Include(r => r.Vehicle.VehicleCategory)
                .Include(r => r.StartStation)
                .FirstOrDefault(r => r.Id == id);

            if (rental == null)
            {
                return HttpNotFound();
            }

            // only owner or admin can return
            if (!User.IsInRole("Admin") && rental.UserId != User.Identity.GetUserId())
            {
                return new HttpUnauthorizedResult();
            }

            ViewBag.Stations = new SelectList(db.Stations.OrderBy(s => s.Name).ToList(), "Id", "Name");

            return View(rental);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessReturn(int Id, int EndStationId)
        {
            var rental = db.Rentals
                .Include(r => r.Vehicle.VehicleCategory)
                .FirstOrDefault(r => r.Id == Id);

            var returnStation = db.Stations.Find(EndStationId);

            if (rental == null || returnStation == null)
            {
                return HttpNotFound();
            }

            // only owner or admin can process
            if (!User.IsInRole("Admin") && rental.UserId != User.Identity.GetUserId())
            {
                return new HttpUnauthorizedResult();
            }

            if (returnStation.CurrentInventory >= returnStation.Capacity)
            {
                TempData["Error"] = "This station is full. Please choose another return station.";
                ViewBag.Stations = new SelectList(db.Stations.OrderBy(s => s.Name).ToList(), "Id", "Name");
                return View("ReturnVehicle", rental);
            }

            var pricingService = new PricingService();
            var endTime = DateTime.Now;
            var baseFare = pricingService.CalculateBaseFare(
                rental.StartTime,
                endTime,
                rental.Vehicle.VehicleCategory.PricePerMinute
            );

            var discount = pricingService.CalculateDiscount(baseFare, returnStation);
            var totalFare = pricingService.CalculateTotalFare(baseFare, discount);

            rental.EndTime = endTime;
            rental.ReturnStationId = returnStation.Id;
            rental.BaseFare = baseFare;
            rental.DiscountAmount = discount;
            rental.TotalFare = totalFare;

            db.SaveChanges();

            // redirect to checkout where payment is confirmed
            return RedirectToAction("Checkout", new { id = rental.Id, userType = "Local" });
        }

        private IEnumerable<SelectListItem> GetStationOptions()
        {
            return db.Stations
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name + " (" + s.CurrentInventory + "/" + s.Capacity + ")"
                })
                .ToList();
        }

        private List<PaymentMethod> GetAllowedPaymentMethods(string userType)
        {
            if (userType == "Tourist")
            {
                return new List<PaymentMethod>
                {
                    PaymentMethod.PayPal,
                    PaymentMethod.ApplePay,
                    PaymentMethod.Cash
                };
            }

            return new List<PaymentMethod>
            {
                PaymentMethod.MoMo,
                PaymentMethod.VNPay,
                PaymentMethod.Cash
            };
        }

        private IEnumerable<SelectListItem> GetPaymentOptions(string userType)
        {
            return GetAllowedPaymentMethods(userType)
                .Select(m => new SelectListItem
                {
                    Value = m.ToString(),
                    Text = m.ToString()
                })
                .ToList();
        }
    }
}
