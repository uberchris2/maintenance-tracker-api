using common;
using common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api_public.Functions;

public class VehicleMaintenance(Container container, ILogger<VehicleMaintenance> logger)
{
    [Function("VehicleMaintenanceGet")]
    public async Task<IActionResult> VehicleMaintenanceGet(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicleMaintenance/{id}")] HttpRequest request,
        string id)
    {
        var parsedId = Guid.Parse(id);

        // Cross-partition query — no PartitionKey set intentionally for public shared-vehicle lookup
        var query = container
            .GetItemLinqQueryable<VehicleMaintenanceModel>()
            .Where(x => x.id == parsedId || x.VehicleId == parsedId)
            .ToFeedIterator();

        var records = new List<VehicleMaintenanceModel>();
        while (query.HasMoreResults)
            records.AddRange(await query.ReadNextAsync(request.HttpContext.RequestAborted));

        var vehicleModel = records.SingleOrDefault(vm => vm.Type == VehicleMaintenanceTypes.Vehicle);
        if (vehicleModel is null || !vehicleModel.Shared)
            return new NotFoundResult();

        var vehicle = vehicleModel.ToVehicleMaintenanceDto();
        vehicle.Maintenance = records
            .Where(vm => vm.Type == VehicleMaintenanceTypes.Maintenance)
            .Select(vm => vm.ToMaintenanceDto())
            .OrderByDescending(m => m.Date);
        logger.LogInformation("Got vehicle maintenance for anonymous user at {RemoteIp}", request.HttpContext.Connection.RemoteIpAddress);
        return new OkObjectResult(vehicle);
    }
}
