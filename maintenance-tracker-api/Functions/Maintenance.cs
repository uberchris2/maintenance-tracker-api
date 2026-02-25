using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using common.Models;
using maintenance_tracker_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api.Functions
{
    public class Maintenance
    {
        private readonly IB2cHelper _b2cHelper;
        private readonly IMapper _mapper;
        private readonly CosmosClient _cosmosClient;
        private readonly ILogger<Maintenance> _logger;

        public Maintenance(IB2cHelper b2cHelper, IMapper mapper, CosmosClient cosmosClient, ILogger<Maintenance> logger)
        {
            _b2cHelper = b2cHelper;
            _mapper = mapper;
            _cosmosClient = cosmosClient;
            _logger = logger;
        }

        private Container GetContainer() =>
            _cosmosClient.GetDatabase("MaintenanceDB").GetContainer("VehicleMaintenance");

        [Function("MaintenanceUpsert")]
        public async Task<IActionResult> MaintenanceUpsert(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "maintenance")] HttpRequest request)
        {
            var dto = await request.ReadFromJsonAsync<MaintenanceDto>();
            var maintenance = _mapper.Map<MaintenanceModel>(dto);
            if (maintenance.id == Guid.Empty)
                maintenance.id = Guid.NewGuid();
            maintenance.UserId = _b2cHelper.GetOid(request);
            maintenance.Type = VehicleMaintenanceTypes.Maintenance;
            await GetContainer().UpsertItemAsync(maintenance, new PartitionKey(maintenance.UserId.ToString()),
                cancellationToken: request.HttpContext.RequestAborted);
            _logger.LogInformation($"Upserting maintenance id {maintenance.id} for user {maintenance.UserId}");
            return new OkResult();
        }

        [Function("MaintenanceDelete")]
        public async Task<IActionResult> MaintenanceDelete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "maintenance/{id}")] HttpRequest request,
            string id)
        {
            var userId = _b2cHelper.GetOid(request);
            await GetContainer().DeleteItemAsync<MaintenanceModel>(id, new PartitionKey(userId.ToString()),
                cancellationToken: request.HttpContext.RequestAborted);
            _logger.LogInformation($"Deleted maintenance id {id} for user {userId}");
            return new OkResult();
        }

        [Function("MaintenanceGet")]
        public async Task<IActionResult> MaintenanceGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "maintenance/{id}")] HttpRequest request,
            string id)
        {
            var userId = _b2cHelper.GetOid(request);
            var response = await GetContainer().ReadItemAsync<MaintenanceModel>(id, new PartitionKey(userId.ToString()),
                cancellationToken: request.HttpContext.RequestAborted);
            var maintenance = _mapper.Map<MaintenanceDto>(response.Resource);
            _logger.LogInformation($"Got maintenance id {id} for user {userId}");
            return new OkObjectResult(maintenance);
        }
    }
}
