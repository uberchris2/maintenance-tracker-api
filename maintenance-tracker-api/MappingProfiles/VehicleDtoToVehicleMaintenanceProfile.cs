using AutoMapper;
using maintenance_tracker_api.Models;

namespace maintenance_tracker_api.MappingProfiles
{
    class VehicleDtoToVehicleMaintenanceProfile : Profile
    {
        public VehicleDtoToVehicleMaintenanceProfile()
        {
            CreateMap<VehicleDto, VehicleMaintenance>();
        }
    }
}
