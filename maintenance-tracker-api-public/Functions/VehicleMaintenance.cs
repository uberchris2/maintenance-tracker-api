using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api_public.Functions
{
    public class VehicleMaintenance
    {
        private readonly IMapper _mapper;
        private readonly CosmosClient _cosmosClient;
        private readonly ILogger<VehicleMaintenance> _logger;

        public VehicleMaintenance(IMapper mapper, CosmosClient cosmosClient, ILogger<VehicleMaintenance> logger)
        {
            _mapper = mapper;
            _cosmosClient = cosmosClient;
            _logger = logger;
        }

        [Function("VehicleMaintenanceGet")]
        public async Task<IActionResult> VehicleMaintenanceGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicleMaintenance/{id}")] HttpRequest request,
            string id)
        {
            var parsedId = Guid.Parse(id);

            // Cross-partition query — no PartitionKey set, which is the v3 equivalent of EnableCrossPartitionQuery = true
            var query = _cosmosClient.GetDatabase("MaintenanceDB").GetContainer("VehicleMaintenance")
                .GetItemLinqQueryable<VehicleMaintenanceModel>()
                .Where(x => x.id == parsedId || x.VehicleId == parsedId)
                .ToFeedIterator();

            var vehiclesAndMaintenance = new List<VehicleMaintenanceModel>();
            while (query.HasMoreResults)
                vehiclesAndMaintenance.AddRange(await query.ReadNextAsync(request.HttpContext.RequestAborted));

            var vehicle = _mapper.Map<VehicleMaintenanceDto>(vehiclesAndMaintenance.Single(vm => vm.Type == VehicleMaintenanceTypes.Vehicle));
            if (!vehicle.Shared)
                return new BadRequestResult();

            vehicle.Maintenance = _mapper
                .Map<IEnumerable<MaintenanceDto>>(vehiclesAndMaintenance.Where(vm => vm.Type == VehicleMaintenanceTypes.Maintenance))
                .OrderByDescending(m => m.Date);
            _logger.LogInformation($"Got vehicle maintenance for anonymous user at {request.HttpContext.Connection.RemoteIpAddress}");
            return new OkObjectResult(vehicle);
        }
    }
}
