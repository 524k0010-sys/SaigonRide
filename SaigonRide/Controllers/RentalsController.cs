using System;
using System.Collections.Generic;
using System.Data;
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
            var currentUserId = User.Identity.GetUserId();
            if (HasUnfinishedRental(currentUserId))
            {
                return TripStartError("Please finish and pay for your current trip before starting another one.");
            }

            Rental rental;
            Vehicle vehicle;

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                vehicle = db.Vehicles
                    .Include(v => v.Station)
                    .FirstOrDefault(v => v.Id == id);

                if (vehicle == null)
                {
                    return TripStartError("Vehicle not found.");
                }

                if (vehicle.Status != VehicleStatus.Ready)
                {
                    return TripStartError("This vehicle is not available.");
                }

                var station = db.Stations.Find(vehicle.StationId);

                rental = new Rental
                {
                    UserId = currentUserId,
                    VehicleId = vehicle.Id,
                    StartStationId = vehicle.StationId,
                    StartTime = DateTime.Now,
                    Status = RentalStatus.Active
                };

                vehicle.Status = VehicleStatus.InTransit;

                db.Entry(vehicle).State = EntityState.Modified;

                if (station != null && station.CurrentInventory > 0)
                {
                    station.CurrentInventory -= 1;
                    db.Entry(station).State = EntityState.Modified;
                }

                db.Rentals.Add(rental);
                db.SaveChanges();
                transaction.Commit();
            }

            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true, rentalId = rental.Id });
            }

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

            if (rental.EndTime.HasValue)
            {
                return RedirectToCheckoutOrSuccess(rental.Id, "Local");
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

            if (!CanAccessRental(rental))
            {
                return new HttpUnauthorizedResult();
            }

            if (rental.EndTime.HasValue)
            {
                return RedirectToCheckoutOrSuccess(rental.Id, "Local");
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

            if (!CanAccessRental(rental))
            {
                return new HttpUnauthorizedResult();
            }

            if (rental.EndTime.HasValue)
            {
                return RedirectToCheckoutOrSuccess(rental.Id, model.UserType);
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
            rental.Status = RentalStatus.Active;

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

            if (!CanAccessRental(rental))
            {
                return new HttpUnauthorizedResult();
            }

            if (!rental.EndTime.HasValue)
            {
                return RedirectToAction("TripInProgress", new { id = rental.Id });
            }

            var model = BuildCheckoutViewModel(rental, GetRentalUserType(rental));

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPayment(CheckoutViewModel model)
        {
            // load rental together with its vehicle and return station to ensure updates are tracked
            var rental = db.Rentals
                .Include(r => r.Vehicle.VehicleCategory)
                .Include(r => r.ReturnStation)
                .FirstOrDefault(r => r.Id == model.RentalId);

            if (rental == null)
            {
                return HttpNotFound();
            }

            if (!CanAccessRental(rental))
            {
                return new HttpUnauthorizedResult();
            }

            var existingPayment = db.Payments.FirstOrDefault(p => p.RentalId == rental.Id && p.Status == PaymentStatus.Success);
            if (existingPayment != null)
            {
                return RedirectToAction("Success", new { id = rental.Id });
            }

            if (!rental.EndTime.HasValue || rental.ReturnStation == null)
            {
                TempData["Error"] = "Please return the vehicle before checkout.";
                return RedirectToAction("ReturnVehicle", new { id = rental.Id });
            }

            var normalizedUserType = GetRentalUserType(rental);
            var allowedMethods = GetAllowedPaymentMethods(normalizedUserType);

            if (!allowedMethods.Contains(model.PaymentMethod))
            {
                ModelState.AddModelError("PaymentMethod", "This payment method is not allowed for this user type.");
            }

            if (!ModelState.IsValid)
            {
                var checkoutModel = BuildCheckoutViewModel(rental, normalizedUserType);
                checkoutModel.PaymentMethod = model.PaymentMethod;
                return View("Checkout", checkoutModel);
            }

            var gateway = new PaymentGatewayService();
            var result = gateway.Pay(model.PaymentMethod, rental.Id, rental.TotalFare);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("PaymentMethod", result.Message);
                var checkoutModel = BuildCheckoutViewModel(rental, normalizedUserType);
                checkoutModel.PaymentMethod = model.PaymentMethod;
                return View("Checkout", checkoutModel);
            }

            var payment = new Payment
            {
                RentalId = rental.Id,
                Method = model.PaymentMethod,
                Status = result.Status,
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

            rental.Status = RentalStatus.Completed;
            db.Payments.Add(payment);
            db.SaveChanges();

            return RedirectToAction("Success", new { id = rental.Id });
        }

        public ActionResult Success(int id)
        {
            var payment = db.Payments
                .Include(p => p.Rental.Vehicle.VehicleCategory)
                .Include(p => p.Rental.ReturnStation)
                .FirstOrDefault(p => p.RentalId == id && p.Status == PaymentStatus.Success);

            if (payment == null)
            {
                return HttpNotFound();
            }

            if (!CanAccessRental(payment.Rental))
            {
                return new HttpUnauthorizedResult();
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

        // GET: Rentals/ReturnVehicles
        public ActionResult ReturnVehicles()
        {
            var currentUserId = User.Identity.GetUserId();

            var rentals = db.Rentals
                .Include(r => r.Vehicle.VehicleCategory)
                .Include(r => r.StartStation)
                .Where(r =>
                    r.UserId == currentUserId &&
                    !r.EndTime.HasValue &&
                    r.Status != RentalStatus.Cancelled)
                .OrderByDescending(r => r.StartTime)
                .ToList();

            return View(rentals);
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

            if (rental.EndTime.HasValue)
            {
                return RedirectToCheckoutOrSuccess(rental.Id, "Local");
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

            if (rental.EndTime.HasValue)
            {
                return RedirectToCheckoutOrSuccess(rental.Id, "Local");
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
            rental.Status = RentalStatus.Active;

            db.SaveChanges();

            // redirect to checkout where payment is confirmed
            return RedirectToAction("Checkout", new { id = rental.Id });
        }

        private bool CanAccessRental(Rental rental)
        {
            return rental != null && (User.IsInRole("Admin") || rental.UserId == User.Identity.GetUserId());
        }

        private bool HasUnfinishedRental(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            return db.Rentals.Any(r =>
                r.UserId == userId &&
                r.Status != RentalStatus.Cancelled &&
                (!r.EndTime.HasValue ||
                 !db.Payments.Any(p => p.RentalId == r.Id && p.Status == PaymentStatus.Success)));
        }

        private ActionResult TripStartError(string message)
        {
            if (Request.IsAjaxRequest())
            {
                return Json(new { success = false, message });
            }

            TempData["Error"] = message;
            return RedirectToAction("Available");
        }

        private ActionResult RedirectToCheckoutOrSuccess(int rentalId, string userType)
        {
            if (db.Payments.Any(p => p.RentalId == rentalId && p.Status == PaymentStatus.Success))
            {
                return RedirectToAction("Success", new { id = rentalId });
            }

            return RedirectToAction("Checkout", new { id = rentalId });
        }

        private CheckoutViewModel BuildCheckoutViewModel(Rental rental, string userType)
        {
            var normalizedUserType = NormalizeUserType(userType);
            var duration = rental.EndTime.HasValue
                ? (int)Math.Ceiling((rental.EndTime.Value - rental.StartTime).TotalMinutes)
                : 0;

            return new CheckoutViewModel
            {
                RentalId = rental.Id,
                VehicleCode = rental.Vehicle?.VehicleCode,
                VehicleCategory = rental.Vehicle?.VehicleCategory?.Name,
                ReturnStationName = rental.ReturnStation?.Name,
                UserType = normalizedUserType,
                DurationMinutes = duration,
                BaseFare = rental.BaseFare,
                DiscountAmount = rental.DiscountAmount,
                TotalFare = rental.TotalFare,
                PaymentMethods = GetPaymentOptions(normalizedUserType)
            };
        }

        private string NormalizeUserType(string userType)
        {
            return string.Equals(userType, "Tourist", StringComparison.OrdinalIgnoreCase) ? "Tourist" : "Local";
        }

        private string GetRentalUserType(Rental rental)
        {
            if (rental == null || string.IsNullOrWhiteSpace(rental.UserId))
            {
                return "Local";
            }

            var user = db.Users.Find(rental.UserId);
            return NormalizeUserType(user?.UserType);
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
            if (NormalizeUserType(userType) == "Tourist")
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
