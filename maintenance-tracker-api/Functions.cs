using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using maintenance_tracker_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api
{
    public static class Functions
    {
        static Functions()
        {
            MappingInitializer.Activate();
        }

        [FunctionName("VehiclesPost")]
        public static void VehiclesPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "vehicles")] VehicleDto req,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "VehicleMaintenance",
                ConnectionStringSetting = "CosmosDBConnection")]out VehicleMaintenance vehicle,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            vehicle = Mapper.Instance.Map<VehicleMaintenance>(req);
            vehicle.id = Guid.NewGuid();
            vehicle.UserId = principal.Identity.Name;
            vehicle.Type = VehicleMaintenanceTypes.Vehicle;
            log.LogInformation($"Saving new vehicle id {vehicle.id} for user {principal.Identity.Name}");
        }

        [FunctionName("VehiclesGet")]
        public static IActionResult VehiclesGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles")] HttpRequest request,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            var uri = UriFactory.CreateDocumentCollectionUri("MaintenanceDB", "VehicleMaintenance");
            var vehicles = client.CreateDocumentQuery<VehicleMaintenance>(uri) //TODO async
                .Where(x => x.UserId == principal.Identity.Name && x.Type == VehicleMaintenanceTypes.Vehicle);
            var mappedVehicles = Mapper.Instance.Map<IEnumerable<VehicleDto>>(vehicles);
            log.LogInformation($"Got all vehicles for user {principal.Identity.Name}");
            return new OkObjectResult(mappedVehicles);
        }

        [FunctionName("VehicleGet")]
        public static async Task<IActionResult> VehicleGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles/{id}")] HttpRequest request,
            string id,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal,
            CancellationToken token
        )
        {
            var uri = UriFactory.CreateDocumentUri("MaintenanceDB", "VehicleMaintenance", id);
            var options = new RequestOptions {PartitionKey = new PartitionKey(principal.Identity.Name)};
            var documentResponse = await client.ReadDocumentAsync<VehicleMaintenance>(uri, options, token);
            var vehicle = Mapper.Instance.Map<VehicleDto>(documentResponse.Document);
            log.LogInformation($"Got vehicle id {id} for user {principal.Identity.Name}");
            return new OkObjectResult(vehicle);
        }

        [FunctionName("VehicleMaintenanceGet")]
        public static IActionResult VehicleMaintenanceGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicleMaintenance/{id}")] HttpRequest request,
            string id,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            var uri = UriFactory.CreateDocumentCollectionUri("MaintenanceDB", "VehicleMaintenance");
            var parsedId = Guid.Parse(id);
            var vehiclesAndMaintenance = client.CreateDocumentQuery<VehicleMaintenance>(uri) //TODO async
                .Where(x => x.UserId == principal.Identity.Name && (x.id == parsedId || x.VehicleId == parsedId)).ToList();
            var vehicle = Mapper.Instance.Map<VehicleMaintenanceDto>(vehiclesAndMaintenance.Single(vm => vm.Type == VehicleMaintenanceTypes.Vehicle));
            vehicle.Maintenance = Mapper.Instance.Map<IEnumerable<MaintenanceDto>>(vehiclesAndMaintenance.Where(vm => vm.Type == VehicleMaintenanceTypes.Maintenance));
            log.LogInformation($"Got all vehicles for user {principal.Identity.Name}");
            return new OkObjectResult(vehicle);
        }

        [FunctionName("MaintenancePost")]
        public static void MaintenancePost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "maintenance")] MaintenanceDto req,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "VehicleMaintenance",
                ConnectionStringSetting = "CosmosDBConnection")]out VehicleMaintenance maintenance,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            maintenance = Mapper.Instance.Map<VehicleMaintenance>(req); ;
            maintenance.id = Guid.NewGuid();
            maintenance.UserId = principal.Identity.Name;
            maintenance.Type = VehicleMaintenanceTypes.Maintenance;
            log.LogInformation($"Saving new maintenance id {maintenance.id} for user {principal.Identity.Name}");
        }

        [FunctionName("MaintenanceGetByVehicle")]
        public static IActionResult MaintenanceGetByVehicle(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "maintenance")] HttpRequest request,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            var uri = UriFactory.CreateDocumentCollectionUri("MaintenanceDB", "VehicleMaintenance");
            var maintenance = client.CreateDocumentQuery<VehicleMaintenance>(uri)
                .Where(p => p.UserId == principal.Identity.Name && p.VehicleId == Guid.Parse(request.Query["vehicleId"]) && p.Type == VehicleMaintenanceTypes.Maintenance);
            var mappedMaintenance = Mapper.Instance.Map<IEnumerable<MaintenanceDto>>(maintenance);
            log.LogInformation($"Got all maintenance on vehicle {request.Query["vehicleId"]} for user {principal.Identity.Name}");
            return new OkObjectResult(mappedMaintenance);
        }
    }
}