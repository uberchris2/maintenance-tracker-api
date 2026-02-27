using System.Net;
using common;
using common.Models;
using maintenance_tracker_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api.Functions;

public class Maintenance(IB2cHelper b2cHelper, Container container, ILogger<Maintenance> logger)
{
    [Function("MaintenanceUpsert")]
    public async Task<IActionResult> MaintenanceUpsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "maintenance")] HttpRequest request)
    {
        var dto = await request.ReadFromJsonAsync<MaintenanceDto>();
        var maintenance = dto!.ToModel();
        if (maintenance.id == Guid.Empty)
            maintenance.id = Guid.NewGuid();
        maintenance.UserId = b2cHelper.GetOid(request);
        maintenance.Type = VehicleMaintenanceTypes.Maintenance;
        await container.UpsertItemAsync(maintenance, new PartitionKey(maintenance.UserId.ToString()),
            cancellationToken: request.HttpContext.RequestAborted);
        logger.LogInformation("Upserting maintenance {MaintenanceId} for user {UserId}", maintenance.id, maintenance.UserId);
        return new OkResult();
    }

    [Function("MaintenanceDelete")]
    public async Task<IActionResult> MaintenanceDelete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "maintenance/{id}")] HttpRequest request,
        string id)
    {
        var userId = b2cHelper.GetOid(request);
        await container.DeleteItemAsync<MaintenanceModel>(id, new PartitionKey(userId.ToString()),
            cancellationToken: request.HttpContext.RequestAborted);
        logger.LogInformation("Deleted maintenance {MaintenanceId} for user {UserId}", id, userId);
        return new OkResult();
    }

    [Function("MaintenanceGet")]
    public async Task<IActionResult> MaintenanceGet(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "maintenance/{id}")] HttpRequest request,
        string id)
    {
        var userId = b2cHelper.GetOid(request);
        try
        {
            var response = await container.ReadItemAsync<MaintenanceModel>(id, new PartitionKey(userId.ToString()),
                cancellationToken: request.HttpContext.RequestAborted);
            logger.LogInformation("Got maintenance {MaintenanceId} for user {UserId}", id, userId);
            return new OkObjectResult(response.Resource.ToDto());
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundResult();
        }
    }
}
