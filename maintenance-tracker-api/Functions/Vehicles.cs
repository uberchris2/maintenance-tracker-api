using System;
using System.Linq;
using System.Security.Claims;
using maintenance_tracker_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api.Functions
{
    public static class Vehicles
    {
        [FunctionName("VehiclesPost")]
        public static void VehiclesPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "vehicles")] Vehicle req,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "Vehicles",
                ConnectionStringSetting = "CosmosDBConnection",
                CreateIfNotExists = true)]out Vehicle vehicle,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            vehicle = req;
            vehicle.id = Guid.NewGuid();
            vehicle.UserId = principal.Identity.Name;
            log.LogInformation("Saving new vehicle");
        }

        [FunctionName("VehiclesGet")]
        public static IActionResult VehiclesGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles")] HttpRequest request,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri("MaintenanceDB", "Vehicles");
            var vehicles = client.CreateDocumentQuery<Vehicle>(collectionUri)
                .Where(p => p.UserId == principal.Identity.Name)
                .ToList();
            log.LogInformation("Got all vehicles");
            return new OkObjectResult(vehicles);
        }

        [FunctionName("VehicleGet")]
        public static IActionResult VehicleGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles/{id}")] HttpRequest request,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "Vehicles",
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}")] Vehicle vehicle,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            if (principal.Identity.Name != vehicle.UserId)
            {
                return new UnauthorizedResult();
            }

            log.LogInformation("Got a vehicle");
            return new OkObjectResult(vehicle);
        }
    }
}