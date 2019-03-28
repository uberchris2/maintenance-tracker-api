using System;
using System.Collections.Generic;
using System.Security.Claims;
using maintenance_tracker_api.Models;
using Microsoft.AspNetCore.Http;
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
            vehicle.id = Guid.NewGuid(); //do I need to set this
            vehicle.UserId = principal.Identity.Name;
            log.LogInformation("Saving new vehicle");
        }

        [FunctionName("VehiclesGet")]
        public static IEnumerable<Vehicle> VehiclesGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles")] HttpRequest request,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "Vehicles",
                ConnectionStringSetting = "CosmosDBConnection")] IEnumerable<Vehicle> vehicles,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            log.LogInformation("Got all vehicles");
            return vehicles;
        }
    }
}