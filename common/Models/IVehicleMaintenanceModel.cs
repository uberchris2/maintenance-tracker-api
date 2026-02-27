namespace common.Models;

public interface IVehicleMaintenanceModel
{
    Guid id { get; set; }
    string Type { get; set; }
    Guid UserId { get; set; }
}
