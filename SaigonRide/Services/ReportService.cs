using System.Collections.Generic;
using System.Linq;
using SaigonRide.Models;
using SaigonRide.ViewModels;

namespace SaigonRide.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext db;

        public ReportService(ApplicationDbContext context)
        {
            db = context;
        }

        public List<StationInventoryReportViewModel> GetStationInventoryReport()
        {
            return db.Stations.Select(s => new StationInventoryReportViewModel
            {
                StationName = s.Name,
                Capacity = s.Capacity,
                CurrentInventory = s.CurrentInventory,
                UtilizationPercent = s.Capacity == 0 ? 0 : ((decimal)s.CurrentInventory / s.Capacity) * 100,
                ReadyVehicles = s.Vehicles.Count(v => v.Status == VehicleStatus.Ready),
                MaintenanceVehicles = s.Vehicles.Count(v => v.Status == VehicleStatus.Maintenance)
            }).ToList();
        }

        public List<RevenueByCategoryReportViewModel> GetRevenueByCategoryReport()
        {
            return db.Rentals
                .Where(r => r.EndTime != null)
                .GroupBy(r => r.Vehicle.VehicleCategory.Name)
                .Select(g => new RevenueByCategoryReportViewModel
                {
                    CategoryName = g.Key,
                    TotalRentals = g.Count(),
                    TotalRevenue = g.Sum(r => r.TotalFare)
                })
                .ToList();
        }
    }
}
