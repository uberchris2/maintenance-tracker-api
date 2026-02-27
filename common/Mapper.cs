using common.Models;

namespace common;

public static class Mapper
{
    public static VehicleModel ToModel(this VehicleDto dto) => new()
    {
        id = dto.id,
        UserId = dto.UserId,
        Year = dto.Year,
        Make = dto.Make,
        Model = dto.Model,
        Name = dto.Name,
        Mileage = dto.Mileage,
        Shared = dto.Shared,
        Vin = dto.Vin,
    };

    public static VehicleDto ToDto(this VehicleModel model) => new()
    {
        id = model.id,
        UserId = model.UserId,
        Year = model.Year,
        Make = model.Make,
        Model = model.Model,
        Name = model.Name,
        Mileage = model.Mileage,
        Shared = model.Shared,
        Vin = model.Vin,
    };

    public static MaintenanceModel ToModel(this MaintenanceDto dto) => new()
    {
        id = dto.id,
        VehicleId = dto.VehicleId,
        Item = dto.Item,
        Mileage = dto.Mileage,
        Date = dto.Date,
        Notes = dto.Notes,
        Receipt = dto.Receipt,
        IntervalMonths = dto.IntervalMonths,
        IntervalMileage = dto.IntervalMileage,
    };

    public static MaintenanceDto ToDto(this MaintenanceModel model) => new()
    {
        id = model.id,
        VehicleId = model.VehicleId,
        Item = model.Item,
        Mileage = model.Mileage,
        Date = model.Date,
        Notes = model.Notes,
        Receipt = model.Receipt,
        IntervalMonths = model.IntervalMonths,
        IntervalMileage = model.IntervalMileage,
    };

    public static MaintenanceDto ToMaintenanceDto(this VehicleMaintenanceModel model) => new()
    {
        id = model.id,
        VehicleId = model.VehicleId,
        Item = model.Item ?? string.Empty,
        Mileage = model.Mileage,
        Date = model.Date,
        Notes = model.Notes,
        Receipt = model.Receipt,
        IntervalMonths = model.IntervalMonths,
        IntervalMileage = model.IntervalMileage,
    };

    public static VehicleMaintenanceDto ToVehicleMaintenanceDto(this VehicleMaintenanceModel model) => new()
    {
        id = model.id,
        UserId = model.UserId,
        Year = model.Year,
        Make = model.Make ?? string.Empty,
        Model = model.Model ?? string.Empty,
        Name = model.Name ?? string.Empty,
        Mileage = model.Mileage,
        Shared = model.Shared,
        Vin = model.Vin,
    };
}
