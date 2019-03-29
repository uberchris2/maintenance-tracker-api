using AutoMapper;
using maintenance_tracker_api.MappingProfiles;

namespace maintenance_tracker_api
{
    public static class MappingInitializer
    {
        public static void Activate()
        {
            Mapper.Initialize(config =>
            {
                config.AddProfile(new VehicleDtoToVehicleMaintenanceProfile());
                config.AddProfile(new VehicleMaintenanceToVehicleDtoProfile());
                config.AddProfile(new MaintenanceDtoToVehicleMaintenanceProfile());
                config.AddProfile(new VehicleMaintenanceToMaintenanceDtoProfile());
            });
        }
    }
}
