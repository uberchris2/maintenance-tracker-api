using AutoMapper;
using common.Models;

namespace common
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
