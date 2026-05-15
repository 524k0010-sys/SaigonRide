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

            if (pricePerMinute <= 0)
            {
                throw new ArgumentException("Price per minute must be greater than zero.");
            }

            var minutes = Math.Ceiling((endTime - startTime).TotalMinutes);
            return (decimal)minutes * pricePerMinute;
        }

        public bool IsLowInventoryStation(Station station)
        {
            if (station == null)
            {
                return false;
            }

            if (station.Capacity <= 0)
            {
                return false;
            }

            var inventoryRate = (decimal)station.CurrentInventory / station.Capacity;
            return inventoryRate < 0.20m;
        }

        public decimal CalculateDiscount(decimal baseFare, Station returnStation)
        {
            if (baseFare <= 0)
            {
                return 0;
            }

            return IsLowInventoryStation(returnStation) ? baseFare * 0.15m : 0;
        }

        public decimal CalculateTotalFare(decimal baseFare, decimal discount)
        {
            return Math.Max(0, baseFare - discount);
        }
    }
}
