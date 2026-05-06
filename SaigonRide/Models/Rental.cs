using System;
using System.ComponentModel.DataAnnotations;

namespace SaigonRide.Models
{
    public class Rental
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public int VehicleId { get; set; }

        public int StartStationId { get; set; }

        public int? ReturnStationId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public decimal BaseFare { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TotalFare { get; set; }

        public RentalStatus Status { get; set; }

        public virtual Vehicle Vehicle { get; set; }
        public virtual Station StartStation { get; set; }
        public virtual Station ReturnStation { get; set; }
    }
}
