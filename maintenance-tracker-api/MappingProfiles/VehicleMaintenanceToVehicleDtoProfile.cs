using AutoMapper;
using maintenance_tracker_api.Models;

namespace maintenance_tracker_api.MappingProfiles
{
    class VehicleMaintenanceToVehicleDtoProfile : Profile
    {
        public VehicleMaintenanceToVehicleDtoProfile()
        {
            CreateMap<VehicleMaintenance, VehicleDto>();
        }
    }
}