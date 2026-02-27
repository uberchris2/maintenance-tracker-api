using System.Net;
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

public class Vehicles(IB2cHelper b2cHelper, Container container, ILogger<Vehicles> logger)
{
    [Function("VehiclesPut")]
    public async Task<IActionResult> VehiclesPut(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "vehicles")] HttpRequest request)
    {
        var dto = await request.ReadFromJsonAsync<VehicleDto>();
        var vehicle = dto!.ToModel();
        if (vehicle.id == Guid.Empty)
            vehicle.id = Guid.NewGuid();
        vehicle.UserId = b2cHelper.GetOid(request);
        vehicle.Type = VehicleMaintenanceTypes.Vehicle;
        await container.UpsertItemAsync(vehicle, new PartitionKey(vehicle.UserId.ToString()),
            cancellationToken: request.HttpContext.RequestAborted);
        logger.LogInformation("Saving vehicle {VehicleId} for user {UserId}", vehicle.id, vehicle.UserId);
        return new OkResult();
    }

    [Function("VehiclesGet")]
    public async Task<IActionResult> VehiclesGet(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles")] HttpRequest request)
    {
        var userId = b2cHelper.GetOid(request);
        var query = container
            .GetItemLinqQueryable<VehicleModel>(
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId.ToString()) })
            .Where(x => x.UserId == userId && x.Type == VehicleMaintenanceTypes.Vehicle)
            .ToFeedIterator();

        var vehicles = new List<VehicleModel>();
        while (query.HasMoreResults)
            vehicles.AddRange(await query.ReadNextAsync(request.HttpContext.RequestAborted));

        logger.LogInformation("Got all vehicles for user {UserId}", userId);
        return new OkObjectResult(vehicles.Select(v => v.ToDto()));
    }

    [Function("VehicleGet")]
    public async Task<IActionResult> VehicleGet(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles/{id}")] HttpRequest request,
        string id)
    {
        var userId = b2cHelper.GetOid(request);
        try
        {
            var response = await container.ReadItemAsync<VehicleModel>(id, new PartitionKey(userId.ToString()),
                cancellationToken: request.HttpContext.RequestAborted);
            logger.LogInformation("Got vehicle {VehicleId} for user {UserId}", id, userId);
            return new OkObjectResult(response.Resource.ToDto());
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundResult();
        }
    }

    [Function("VehicleDelete")]
    public async Task<IActionResult> VehicleDelete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "vehicles/{id}")] HttpRequest request,
        string id)
    {
        var userId = b2cHelper.GetOid(request);
        var parsedId = Guid.Parse(id);

        var query = container
            .GetItemLinqQueryable<VehicleMaintenanceModel>(
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId.ToString()) })
            .Where(m => (m.VehicleId == parsedId || m.id == parsedId) && m.UserId == userId)
            .ToFeedIterator();

        var records = new List<VehicleMaintenanceModel>();
        while (query.HasMoreResults)
            records.AddRange(await query.ReadNextAsync(request.HttpContext.RequestAborted));

        await Task.WhenAll(records.Select(m =>
            container.DeleteItemAsync<VehicleMaintenanceModel>(m.id.ToString(), new PartitionKey(userId.ToString()),
                cancellationToken: request.HttpContext.RequestAborted)));
        logger.LogInformation("Deleted vehicle {VehicleId} for user {UserId}", id, userId);
        return new OkResult();
    }
}
