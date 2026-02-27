namespace common.Models;

public record MaintenanceModel : IVehicleMaintenanceModel
{
    public Guid id { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; }
    public string Item { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string? Receipt { get; set; }
    public int IntervalMonths { get; set; }
    public int IntervalMileage { get; set; }
}