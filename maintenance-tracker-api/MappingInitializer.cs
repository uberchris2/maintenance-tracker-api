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
                config.CreateMap<VehicleDto, VehicleMaintenanceModel>();
                config.CreateMap<MaintenanceDto, VehicleMaintenanceModel>();
                config.CreateMap<VehicleMaintenanceModel, VehicleDto>();
                config.CreateMap<VehicleMaintenanceModel, MaintenanceDto>();
                config.CreateMap<VehicleMaintenanceModel, VehicleMaintenanceDto>();
            });
        }
    }
}
