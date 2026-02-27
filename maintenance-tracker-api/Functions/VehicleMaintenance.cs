using common;
using common.Models;
using maintenance_tracker_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api.Functions;

public class VehicleMaintenance(IB2cHelper b2cHelper, Container container, ILogger<VehicleMaintenance> logger)
{
    [Function("VehicleMaintenanceGet")]
    public async Task<IActionResult> VehicleMaintenanceGet(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicleMaintenance/{id}")] HttpRequest request,
        string id)
    {
        var userId = b2cHelper.GetOid(request);
        var parsedId = Guid.Parse(id);

        var query = container
            .GetItemLinqQueryable<VehicleMaintenanceModel>(
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId.ToString()) })
            .Where(x => x.UserId == userId && (x.id == parsedId || x.VehicleId == parsedId))
            .ToFeedIterator();

        var records = new List<VehicleMaintenanceModel>();
        while (query.HasMoreResults)
            records.AddRange(await query.ReadNextAsync(request.HttpContext.RequestAborted));

        var vehicleModel = records.SingleOrDefault(vm => vm.Type == VehicleMaintenanceTypes.Vehicle);
        if (vehicleModel is null)
            return new NotFoundResult();

        var vehicle = vehicleModel.ToVehicleMaintenanceDto();
        vehicle.Maintenance = records
            .Where(vm => vm.Type == VehicleMaintenanceTypes.Maintenance)
            .Select(vm => vm.ToMaintenanceDto())
            .OrderByDescending(m => m.Date);
        logger.LogInformation("Got vehicle maintenance for user {UserId}", userId);
        return new OkObjectResult(vehicle);
    }
}
