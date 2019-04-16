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
                config.CreateMap<VehicleDto, VehicleModel>();
                config.CreateMap<MaintenanceDto, MaintenanceModel>();
                config.CreateMap<VehicleModel, VehicleDto>();
                config.CreateMap<MaintenanceModel, MaintenanceDto>();
                config.CreateMap<VehicleMaintenanceModel, VehicleMaintenanceDto>();
            });
        }
    }
}
