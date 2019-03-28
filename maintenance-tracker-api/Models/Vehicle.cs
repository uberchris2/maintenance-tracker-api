using System;

namespace maintenance_tracker_api.Models
{
    public class Vehicle
    {
        // ReSharper disable once InconsistentNaming
        public Guid id { get; set; }
        public string UserId { get; set; }
        public string Year { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }
    }
}
