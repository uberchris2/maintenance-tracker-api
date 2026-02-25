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
    public class VehicleMaintenance
    {
        private readonly IB2cHelper _b2cHelper;
        private readonly IMapper _mapper;
        private readonly CosmosClient _cosmosClient;
        private readonly ILogger<VehicleMaintenance> _logger;

        public VehicleMaintenance(IB2cHelper b2cHelper, IMapper mapper, CosmosClient cosmosClient, ILogger<VehicleMaintenance> logger)
        {
            _b2cHelper = b2cHelper;
            _mapper = mapper;
            _cosmosClient = cosmosClient;
            _logger = logger;
        }

        [Function("VehicleMaintenanceGet")]
        public async Task<IActionResult> VehicleMaintenanceGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicleMaintenance/{id}")] HttpRequest request,
            string id)
        {
            var userId = _b2cHelper.GetOid(request);
            var parsedId = Guid.Parse(id);

            var query = _cosmosClient.GetDatabase("MaintenanceDB").GetContainer("VehicleMaintenance")
                .GetItemLinqQueryable<VehicleMaintenanceModel>(
                    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId.ToString()) })
                .Where(x => x.UserId == userId && (x.id == parsedId || x.VehicleId == parsedId))
                .ToFeedIterator();

            var vehiclesAndMaintenance = new List<VehicleMaintenanceModel>();
            while (query.HasMoreResults)
                vehiclesAndMaintenance.AddRange(await query.ReadNextAsync(request.HttpContext.RequestAborted));

            var vehicle = _mapper.Map<VehicleMaintenanceDto>(vehiclesAndMaintenance.Single(vm => vm.Type == VehicleMaintenanceTypes.Vehicle));
            vehicle.Maintenance = _mapper
                .Map<IEnumerable<MaintenanceDto>>(vehiclesAndMaintenance.Where(vm => vm.Type == VehicleMaintenanceTypes.Maintenance))
                .OrderByDescending(m => m.Date);
            _logger.LogInformation($"Got vehicle maintenance for user {userId}");
            return new OkObjectResult(vehicle);
        }
    }
}
