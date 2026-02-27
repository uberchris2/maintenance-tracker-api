namespace common.Models;

public record VehicleMaintenanceModel : IVehicleMaintenanceModel
{
    public Guid id { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    // Vehicle fields
    public int Year { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Name { get; set; }
    public bool Shared { get; set; }
    public string? Vin { get; set; }
    // Maintenance fields
    public Guid VehicleId { get; set; }
    public string? Item { get; set; }
    public int Mileage { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string? Receipt { get; set; }
    public int IntervalMonths { get; set; }
    public int IntervalMileage { get; set; }
}
