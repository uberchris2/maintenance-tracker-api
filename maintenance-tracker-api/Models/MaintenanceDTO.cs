using System;

namespace maintenance_tracker_api.Models
{
    public class MaintenanceDto
    {
        public Guid id { get; set; }
        public string VehicleId { get; set; }
        public string Item { get; set; }
        public int Mileage { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }
        public string Receipt { get; set; }
    }
}
