using System;
using System.Collections.Generic;

namespace common.Models
{
    public class VehicleMaintenanceDto
    {
        // ReSharper disable once InconsistentNaming
        public Guid id { get; set; }
        public Guid UserId { get; set; }
        public int Year { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }
        public int Mileage { get; set; }
        public bool Shared { get; set; }
        public string? Vin { get; set; }
        public IEnumerable<MaintenanceDto> Maintenance { get; set; }
    }
}