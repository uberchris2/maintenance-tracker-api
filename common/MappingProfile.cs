using AutoMapper;
using common.Models;

namespace common
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<VehicleDto, VehicleModel>();
            CreateMap<MaintenanceDto, MaintenanceModel>();
            CreateMap<VehicleModel, VehicleDto>();
            CreateMap<MaintenanceModel, MaintenanceDto>();
            CreateMap<VehicleMaintenanceModel, VehicleMaintenanceDto>();
        }
    }
}
