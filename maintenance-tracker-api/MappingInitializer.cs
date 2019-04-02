using AutoMapper;
using maintenance_tracker_api.Models;

namespace maintenance_tracker_api
{
    public static class MappingInitializer
    {
        public static void Activate()
        {
            Mapper.Initialize(config =>
            {
                config.CreateMap<VehicleDto, VehicleMaintenance>();
                config.CreateMap<MaintenanceDto, VehicleMaintenance>();
                config.CreateMap<VehicleMaintenance, VehicleDto>();
                config.CreateMap<VehicleMaintenance, MaintenanceDto>();
                config.CreateMap<VehicleMaintenance, VehicleMaintenanceDto>();
            });
        }
    }
}
