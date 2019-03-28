using System;

namespace maintenance_tracker_api.Models
{
    public class Maintenance
    {
        // ReSharper disable once InconsistentNaming
        public Guid id { get; set; }
        public Guid Vehicle { get; set; }
        public string UserId { get; set; }
        public string Item { get; set; }
        public string Mileage { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }
    }
}
