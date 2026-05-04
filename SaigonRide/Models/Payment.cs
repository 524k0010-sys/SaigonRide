using System;

namespace SaigonRide.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int RentalId { get; set; }

        public PaymentMethod Method { get; set; }

        public PaymentStatus Status { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaidAt { get; set; }

        public virtual Rental Rental { get; set; }
    }
}
