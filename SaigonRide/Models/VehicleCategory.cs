using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SaigonRide.Models
{
    public class VehicleCategory
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public decimal PricePerMinute { get; set; }

        public virtual ICollection<Vehicle> Vehicles { get; set; }
    }
}
