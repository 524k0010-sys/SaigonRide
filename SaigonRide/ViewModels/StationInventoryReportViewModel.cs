namespace SaigonRide.ViewModels
{
    public class StationInventoryReportViewModel
    {
        public string StationName { get; set; }
        public int Capacity { get; set; }
        public int CurrentInventory { get; set; }
        public decimal UtilizationPercent { get; set; }
        public int ReadyVehicles { get; set; }
        public int MaintenanceVehicles { get; set; }
    }
}
