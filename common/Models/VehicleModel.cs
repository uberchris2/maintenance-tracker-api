using System;

namespace common.Models
{
    public class VehicleModel : IVehicleMaintenanceModel
    {
        public Guid id { get; set; }
        public string Type { get; set; }
        public Guid UserId { get; set; }
        public int Year { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }
        public int Mileage { get; set; }
        public bool Shared { get; set; }
    }
}