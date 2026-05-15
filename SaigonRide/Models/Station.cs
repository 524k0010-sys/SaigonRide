using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SaigonRide.Models
{
    public class Station : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }

        [Range(0, int.MaxValue)]
        public int CurrentInventory { get; set; }

        public virtual ICollection<Vehicle> Vehicles { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CurrentInventory > Capacity)
            {
                yield return new ValidationResult(
                    "Current inventory cannot exceed station capacity.",
                    new[] { nameof(CurrentInventory) });
            }
        }
    }
}
