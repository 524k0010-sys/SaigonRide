using System;
using System.Collections.Generic;
using System.Web.Mvc;
using SaigonRide.Models;

namespace SaigonRide.ViewModels
{
    public class CheckoutViewModel
    {
        public int RentalId { get; set; }
        public string VehicleCode { get; set; }
        public string VehicleCategory { get; set; }
        public string ReturnStationName { get; set; }
        public string UserType { get; set; }

        public int DurationMinutes { get; set; }
        public decimal BaseFare { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalFare { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public IEnumerable<SelectListItem> PaymentMethods { get; set; }
    }
}
