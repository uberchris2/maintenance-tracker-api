namespace common.Models;

public record VehicleModel : IVehicleMaintenanceModel
{
    public Guid id { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public int Year { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public bool Shared { get; set; }
    public string? Vin { get; set; }
}