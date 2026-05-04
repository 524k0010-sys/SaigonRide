using System;
using SaigonRide.Models;

namespace SaigonRide.Services
{
    public class PricingService
    {
        public decimal CalculateBaseFare(DateTime startTime, DateTime endTime, decimal pricePerMinute)
        {
            if (endTime <= startTime)
            {
                throw new ArgumentException("End time must be after start time.");
            }

            var minutes = Math.Ceiling((endTime - startTime).TotalMinutes);
            return (decimal)minutes * pricePerMinute;
        }

        public bool IsLowInventoryStation(Station station)
        {
            if (station.Capacity <= 0)
            {
                return false;
            }

            var inventoryRate = (decimal)station.CurrentInventory / station.Capacity;
            return inventoryRate < 0.20m;
        }

        public decimal CalculateDiscount(decimal baseFare, Station returnStation)
        {
            return IsLowInventoryStation(returnStation) ? baseFare * 0.15m : 0;
        }

        public decimal CalculateTotalFare(decimal baseFare, decimal discount)
        {
            return baseFare - discount;
        }
    }
}
