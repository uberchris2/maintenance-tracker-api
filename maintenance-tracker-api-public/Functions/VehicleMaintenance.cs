using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api_public.Functions
{
    public class VehicleMaintenance
    {
        private readonly IMapper _mapper;

        public VehicleMaintenance(IMapper mapper)
        {
            _mapper = mapper;
        }

        [FunctionName("VehicleMaintenanceGet")]
        public IActionResult VehicleMaintenanceGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicleMaintenance/{id}")] HttpRequest request,
            string id,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log
        )
        {
            var uri = UriFactory.CreateDocumentCollectionUri("MaintenanceDB", "VehicleMaintenance");
            var parsedId = Guid.Parse(id);
            var options = new FeedOptions {EnableCrossPartitionQuery = true};
            var vehiclesAndMaintenance = client.CreateDocumentQuery<VehicleMaintenanceModel>(uri, options) //TODO async
                .Where(x => x.id == parsedId || x.VehicleId == parsedId).ToList();
            var vehicle = _mapper.Map<VehicleMaintenanceDto>(vehiclesAndMaintenance.Single(vm => vm.Type == VehicleMaintenanceTypes.Vehicle));
            if (!vehicle.Shared)
            {
                return new BadRequestResult();
            }
            vehicle.Maintenance = _mapper
                .Map<IEnumerable<MaintenanceDto>>(vehiclesAndMaintenance.Where(vm => vm.Type == VehicleMaintenanceTypes.Maintenance))
                .OrderByDescending(m => m.Date);
            log.LogInformation($"Got all vehicles for anonymous user at {request.HttpContext.Connection.RemoteIpAddress}");
            return new OkObjectResult(vehicle);
        }
    }
}