using AutoMapper;
using maintenance_tracker_api.Models;

namespace maintenance_tracker_api.MappingProfiles
{
    class VehicleMaintenanceToMaintenanceDtoProfile : Profile
    {
        public VehicleMaintenanceToMaintenanceDtoProfile()
        {
            CreateMap<VehicleMaintenance, MaintenanceDto>();
        }
    }
}