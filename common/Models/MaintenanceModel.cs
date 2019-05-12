using System;

namespace common.Models
{
    public class MaintenanceModel : IVehicleMaintenanceModel
    {
        public Guid id { get; set; }
        public string Type { get; set; }
        public Guid UserId { get; set; }
        public Guid VehicleId { get; set; }
        public string Item { get; set; }
        public int Mileage { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }
        public string Receipt { get; set; }
        public int IntervalMonths { get; set; }
        public int IntervalMileage { get; set; }
    }
}