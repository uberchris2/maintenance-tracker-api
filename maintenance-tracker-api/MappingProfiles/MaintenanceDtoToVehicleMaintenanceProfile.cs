using AutoMapper;
using maintenance_tracker_api.Models;

namespace maintenance_tracker_api.MappingProfiles
{
    class MaintenanceDtoToVehicleMaintenanceProfile : Profile
    {
        public MaintenanceDtoToVehicleMaintenanceProfile()
        {
            CreateMap<MaintenanceDto, VehicleMaintenance>();
        }
    }
}