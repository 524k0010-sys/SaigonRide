using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SaigonRide.Models
{
    public class Station
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int Capacity { get; set; }

        public int CurrentInventory { get; set; }

        public virtual ICollection<Vehicle> Vehicles { get; set; }
    }
}
