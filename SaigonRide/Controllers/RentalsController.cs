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
            var vehicles = db.Vehicles
                .Include(v => v.Station)
                .Include(v => v.VehicleCategory)
                .Where(v => v.Status == VehicleStatus.Ready)
                .ToList();

            return View(vehicles);
        }

        public ActionResult StartTrip(int id)
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

            if (station != null && station.CurrentInventory > 0)
            {
                station.CurrentInventory -= 1;
            }

            db.Rentals.Add(rental);
            db.SaveChanges();

            return RedirectToAction("EndTrip", new { id = rental.Id });
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
            var rental = db.Rentals
                .Include(r => r.Vehicle)
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

            var returnStation = db.Stations.Find(rental.ReturnStationId);

            rental.Vehicle.Status = VehicleStatus.Ready;

            if (returnStation != null)
            {
                rental.Vehicle.StationId = returnStation.Id;

                if (returnStation.CurrentInventory < returnStation.Capacity)
                {
                    returnStation.CurrentInventory += 1;
                }
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
