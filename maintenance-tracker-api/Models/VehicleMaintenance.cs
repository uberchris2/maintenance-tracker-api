using System;

namespace maintenance_tracker_api.Models
{
    public class VehicleMaintenance
    {
        public Guid id { get; set; }
        public string Type { get; set; }
        public string UserId { get; set; }

        //vehicle fields
        public int Year { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }
        
        //maintenance fields
        public Guid VehicleId { get; set; }
        public string Item { get; set; }
        public int Mileage { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }
    }
}