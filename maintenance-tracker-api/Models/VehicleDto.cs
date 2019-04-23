namespace maintenance_tracker_api.Models
{
    public class VehicleDto
    {
        // ReSharper disable once InconsistentNaming
        public string id { get; set; }
        public string UserId { get; set; }
        public int Year { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Name { get; set; }
        public int Mileage { get; set; }
    }
}
