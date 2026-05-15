using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonRide.Models;
using SaigonRide.Services;

namespace SaigonRide.Tests
{
    [TestClass]
    public class PricingServiceTests
    {
        [TestMethod]
        public void CalculateBaseFare_StandardBike10Minutes_Returns5000()
        {
            var service = new PricingService();
            var start = new DateTime(2026, 5, 5, 10, 0, 0);
            var end = new DateTime(2026, 5, 5, 10, 10, 0);

            var result = service.CalculateBaseFare(start, end, 500m);

            Assert.AreEqual(5000m, result);
        }

        [TestMethod]
        public void CalculateBaseFare_EScooter10Minutes_Returns15000()
        {
            var service = new PricingService();
            var start = new DateTime(2026, 5, 5, 10, 0, 0);
            var end = new DateTime(2026, 5, 5, 10, 10, 0);

            var result = service.CalculateBaseFare(start, end, 1500m);

            Assert.AreEqual(15000m, result);
        }

        [TestMethod]
        public void CalculateDiscount_Inventory19Percent_Returns15PercentDiscount()
        {
            var service = new PricingService();
            var station = new Station
            {
                Capacity = 100,
                CurrentInventory = 19
            };

            var discount = service.CalculateDiscount(100000m, station);

            Assert.AreEqual(15000m, discount);
        }

        [TestMethod]
        public void CalculateDiscount_Inventory20Percent_ReturnsNoDiscount()
        {
            var service = new PricingService();
            var station = new Station
            {
                Capacity = 100,
                CurrentInventory = 20
            };

            var discount = service.CalculateDiscount(100000m, station);

            Assert.AreEqual(0m, discount);
        }

        [TestMethod]
        public void CalculateDiscount_Inventory21Percent_ReturnsNoDiscount()
        {
            var service = new PricingService();
            var station = new Station
            {
                Capacity = 100,
                CurrentInventory = 21
            };

            var discount = service.CalculateDiscount(100000m, station);

            Assert.AreEqual(0m, discount);
        }

        [TestMethod]
        public void CalculateBaseFare_PartialMinute_RoundsUp()
        {
            var service = new PricingService();
            var start = new DateTime(2026, 5, 5, 10, 0, 0);
            var end = new DateTime(2026, 5, 5, 10, 0, 1);

            var result = service.CalculateBaseFare(start, end, 500m);

            Assert.AreEqual(500m, result);
        }

        [TestMethod]
        public void CalculateBaseFare_EndBeforeStart_Throws()
        {
            var service = new PricingService();
            var start = new DateTime(2026, 5, 5, 10, 0, 0);
            var end = new DateTime(2026, 5, 5, 9, 59, 0);

            Assert.ThrowsException<ArgumentException>(() => service.CalculateBaseFare(start, end, 500m));
        }

        [TestMethod]
        public void CalculateBaseFare_ZeroPrice_Throws()
        {
            var service = new PricingService();
            var start = new DateTime(2026, 5, 5, 10, 0, 0);
            var end = new DateTime(2026, 5, 5, 10, 10, 0);

            Assert.ThrowsException<ArgumentException>(() => service.CalculateBaseFare(start, end, 0m));
        }

        [TestMethod]
        public void CalculateDiscount_ZeroCapacity_ReturnsNoDiscount()
        {
            var service = new PricingService();
            var station = new Station
            {
                Capacity = 0,
                CurrentInventory = 0
            };

            var discount = service.CalculateDiscount(100000m, station);

            Assert.AreEqual(0m, discount);
        }

        [TestMethod]
        public void CalculateDiscount_NullStation_ReturnsNoDiscount()
        {
            var service = new PricingService();

            var discount = service.CalculateDiscount(100000m, null);

            Assert.AreEqual(0m, discount);
        }
    }
}
