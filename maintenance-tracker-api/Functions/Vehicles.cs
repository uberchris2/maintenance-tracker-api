using System;
using System.Collections.Generic;
using System.Linq;
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
    public class Vehicles
    {
        private readonly IB2cHelper _b2cHelper;
        private readonly IMapper _mapper;
        private readonly CosmosClient _cosmosClient;
        private readonly ILogger<Vehicles> _logger;

        public Vehicles(IB2cHelper b2cHelper, IMapper mapper, CosmosClient cosmosClient, ILogger<Vehicles> logger)
        {
            _b2cHelper = b2cHelper;
            _mapper = mapper;
            _cosmosClient = cosmosClient;
            _logger = logger;
        }

        private Container GetContainer() =>
            _cosmosClient.GetDatabase("MaintenanceDB").GetContainer("VehicleMaintenance");

        [Function("VehiclesPut")]
        public async Task<IActionResult> VehiclesPut(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "vehicles")] HttpRequest request)
        {
            var dto = await request.ReadFromJsonAsync<VehicleDto>();
            var vehicle = _mapper.Map<VehicleModel>(dto);
            if (vehicle.id == Guid.Empty)
                vehicle.id = Guid.NewGuid();
            vehicle.UserId = _b2cHelper.GetOid(request);
            vehicle.Type = VehicleMaintenanceTypes.Vehicle;
            await GetContainer().UpsertItemAsync(vehicle, new PartitionKey(vehicle.UserId.ToString()),
                cancellationToken: request.HttpContext.RequestAborted);
            _logger.LogInformation($"Saving vehicle id {vehicle.id} for user {vehicle.UserId}");
            return new OkResult();
        }

        [Function("VehiclesGet")]
        public async Task<IActionResult> VehiclesGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles")] HttpRequest request)
        {
            var userId = _b2cHelper.GetOid(request);
            var query = GetContainer()
                .GetItemLinqQueryable<VehicleModel>(
                    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId.ToString()) })
                .Where(x => x.UserId == userId && x.Type == VehicleMaintenanceTypes.Vehicle)
                .ToFeedIterator();

            var vehicles = new List<VehicleModel>();
            while (query.HasMoreResults)
                vehicles.AddRange(await query.ReadNextAsync(request.HttpContext.RequestAborted));

            _logger.LogInformation($"Got all vehicles for user {userId}");
            return new OkObjectResult(_mapper.Map<IEnumerable<VehicleDto>>(vehicles));
        }

        [Function("VehicleGet")]
        public async Task<IActionResult> VehicleGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles/{id}")] HttpRequest request,
            string id)
        {
            var userId = _b2cHelper.GetOid(request);
            var response = await GetContainer().ReadItemAsync<VehicleModel>(id, new PartitionKey(userId.ToString()),
                cancellationToken: request.HttpContext.RequestAborted);
            var vehicle = _mapper.Map<VehicleDto>(response.Resource);
            _logger.LogInformation($"Got vehicle id {id} for user {userId}");
            return new OkObjectResult(vehicle);
        }

        [Function("VehicleDelete")]
        public async Task<IActionResult> VehicleDelete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "vehicle/{id}")] HttpRequest request,
            string id)
        {
            var userId = _b2cHelper.GetOid(request);
            var parsedId = Guid.Parse(id);
            var container = GetContainer();

            var query = container
                .GetItemLinqQueryable<VehicleMaintenanceModel>(
                    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId.ToString()) })
                .Where(m => (m.VehicleId == parsedId || m.id == parsedId) && m.UserId == userId)
                .ToFeedIterator();

            var records = new List<VehicleMaintenanceModel>();
            while (query.HasMoreResults)
                records.AddRange(await query.ReadNextAsync(request.HttpContext.RequestAborted));

            var deleteTasks = records.Select(m =>
                container.DeleteItemAsync<VehicleMaintenanceModel>(m.id.ToString(), new PartitionKey(userId.ToString()),
                    cancellationToken: request.HttpContext.RequestAborted));
            await Task.WhenAll(deleteTasks);
            _logger.LogInformation($"Deleted vehicle id {id} for user {userId}");
            return new OkResult();
        }
    }
}
