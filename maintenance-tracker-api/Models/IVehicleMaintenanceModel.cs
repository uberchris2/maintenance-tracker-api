using System;

namespace maintenance_tracker_api.Models
{
    public interface IVehicleMaintenanceModel
    {
        Guid id { get; set; }
        string Type { get; set; }
        Guid UserId { get; set; }
    }
}