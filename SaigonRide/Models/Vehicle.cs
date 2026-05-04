using System.ComponentModel.DataAnnotations;

namespace SaigonRide.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Vehicle Code")]
        public string VehicleCode { get; set; }

        public int VehicleCategoryId { get; set; }
        public int StationId { get; set; }

        public VehicleStatus Status { get; set; }

        public virtual VehicleCategory VehicleCategory { get; set; }
        public virtual Station Station { get; set; }
    }
}
