using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using maintenance_tracker_api.Models;
using maintenance_tracker_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api.Functions
{
    public class Vehicles
    {
        private readonly IB2cHelper _b2cHelper;
        private readonly IMapper _mapper;

        public Vehicles(IB2cHelper b2cHelper, IMapper mapper)
        {
            _b2cHelper = b2cHelper;
            _mapper = mapper;
        }

        [FunctionName("VehiclesPost")]
        public void VehiclesPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "vehicles")] VehicleDto request,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "VehicleMaintenance",
                ConnectionStringSetting = "CosmosDBConnection")]out VehicleModel vehicle,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            vehicle = _mapper.Map<VehicleModel>(request);
            vehicle.id = Guid.NewGuid();
            vehicle.UserId = _b2cHelper.GetOid(principal);
            vehicle.Type = VehicleMaintenanceTypes.Vehicle;
            log.LogInformation($"Saving new vehicle id {vehicle.id} for user {_b2cHelper.GetOid(principal)}");
        }

        [FunctionName("VehiclesGet")]
        public IActionResult VehiclesGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles")] HttpRequest request,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            var uri = UriFactory.CreateDocumentCollectionUri("MaintenanceDB", "VehicleMaintenance");
            //TODO async this once its implemented https://github.com/Azure/azure-cosmos-dotnet-v2/issues/287
            var vehicles = client.CreateDocumentQuery<VehicleModel>(uri)
                .Where(x => x.UserId == _b2cHelper.GetOid(principal) && x.Type == VehicleMaintenanceTypes.Vehicle);
            var mappedVehicles = _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
            log.LogInformation($"Got all vehicles for user {_b2cHelper.GetOid(principal)}");
            return new OkObjectResult(mappedVehicles);
        }

        [FunctionName("VehicleGet")]
        public async Task<IActionResult> VehicleGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles/{id}")] HttpRequest request,
            string id,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal,
            CancellationToken token
        )
        {
            var uri = UriFactory.CreateDocumentUri("MaintenanceDB", "VehicleMaintenance", id);
            var options = new RequestOptions { PartitionKey = new PartitionKey(_b2cHelper.GetOid(principal).ToString()) };
            var documentResponse = await client.ReadDocumentAsync<VehicleModel>(uri, options, token);
            var vehicle = _mapper.Map<VehicleDto>(documentResponse.Document);
            log.LogInformation($"Got vehicle id {id} for user {_b2cHelper.GetOid(principal)}");
            return new OkObjectResult(vehicle);
        }
    }
}