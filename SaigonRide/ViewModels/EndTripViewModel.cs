using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace SaigonRide.ViewModels
{
    public class EndTripViewModel
    {
        public int RentalId { get; set; }
        public string VehicleCode { get; set; }
        public string StartStationName { get; set; }
        public DateTime StartTime { get; set; }

        public int ReturnStationId { get; set; }
        public string UserType { get; set; }

        public IEnumerable<SelectListItem> ReturnStations { get; set; }
    }
}
