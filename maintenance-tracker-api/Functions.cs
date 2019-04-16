using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "vehicles")] VehicleDto request,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "VehicleMaintenance",
                ConnectionStringSetting = "CosmosDBConnection")]out VehicleMaintenance vehicle,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            vehicle = Mapper.Instance.Map<VehicleMaintenance>(request);
            vehicle.id = Guid.NewGuid();
            vehicle.UserId = B2cHelper.GetOid(principal);
            vehicle.Type = VehicleMaintenanceTypes.Vehicle;
            log.LogInformation($"Saving new vehicle id {vehicle.id} for user {B2cHelper.GetOid(principal)}");
        }

        [FunctionName("VehiclesGet")]
        public static IActionResult VehiclesGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "vehicles")] HttpRequest request,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            var principalClaims = principal.Claims.Select(c => new { c.Type, c.Value, c.ValueType });
            log.LogInformation(JsonConvert.SerializeObject(principalClaims, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));

            var uri = UriFactory.CreateDocumentCollectionUri("MaintenanceDB", "VehicleMaintenance");
            //TODO async this once its implemented https://github.com/Azure/azure-cosmos-dotnet-v2/issues/287
            var vehicles = client.CreateDocumentQuery<VehicleMaintenance>(uri)
                .Where(x => x.UserId == B2cHelper.GetOid(principal) && x.Type == VehicleMaintenanceTypes.Vehicle);
            var mappedVehicles = Mapper.Instance.Map<IEnumerable<VehicleDto>>(vehicles);
            log.LogInformation($"Got all vehicles for user {B2cHelper.GetOid(principal)}");
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
            var options = new RequestOptions {PartitionKey = new PartitionKey(B2cHelper.GetOid(principal).ToString())};
            var documentResponse = await client.ReadDocumentAsync<VehicleMaintenance>(uri, options, token);
            var vehicle = Mapper.Instance.Map<VehicleDto>(documentResponse.Document);
            log.LogInformation($"Got vehicle id {id} for user {B2cHelper.GetOid(principal)}");
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
                .Where(x => x.UserId == B2cHelper.GetOid(principal) && (x.id == parsedId || x.VehicleId == parsedId)).ToList();
            var vehicle = Mapper.Instance.Map<VehicleMaintenanceDto>(vehiclesAndMaintenance.Single(vm => vm.Type == VehicleMaintenanceTypes.Vehicle));
            vehicle.Maintenance = Mapper.Instance
                .Map<IEnumerable<MaintenanceDto>>(vehiclesAndMaintenance.Where(vm => vm.Type == VehicleMaintenanceTypes.Maintenance))
                .OrderByDescending(m => m.Date);
            log.LogInformation($"Got all vehicles for user {B2cHelper.GetOid(principal)}");
            return new OkObjectResult(vehicle);
        }

        [FunctionName("MaintenancePost")]
        public static void MaintenancePost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "maintenance")] MaintenanceDto request,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "VehicleMaintenance",
                ConnectionStringSetting = "CosmosDBConnection")]out VehicleMaintenance maintenance,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            maintenance = Mapper.Instance.Map<VehicleMaintenance>(request); ;
            maintenance.id = Guid.NewGuid();
            maintenance.UserId = B2cHelper.GetOid(principal);
            maintenance.Type = VehicleMaintenanceTypes.Maintenance;
            log.LogInformation($"Saving new maintenance id {maintenance.id} for user {B2cHelper.GetOid(principal)}");
        }

        [FunctionName("VehicleMaintenanceDelete")]
        public static async Task VehicleMaintenanceDelete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "vehicleMaintenance/{id}")] HttpRequest request,
            string id,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal,
            CancellationToken token
        )
        {
            var uri = UriFactory.CreateDocumentUri("MaintenanceDB", "VehicleMaintenance", id);
            var options = new RequestOptions { PartitionKey = new PartitionKey(B2cHelper.GetOid(principal).ToString()) };
            await client.DeleteDocumentAsync(uri, options, token);
            log.LogInformation($"Deleted maintenance id {id} for user {B2cHelper.GetOid(principal)}");
        }

        [FunctionName("ReceiptAuthorizationGet")]
        public static IActionResult ReceiptAuthorizationGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authorizeReceipt")] HttpRequest request,
            [Blob("receipts", FileAccess.ReadWrite, Connection = "UploadStorage")] CloudBlobContainer container,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            var blob = container.GetBlockBlobReference($"{B2cHelper.GetOid(principal)}/{request.Query["name"]}");
            var policy = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
            };
            var sas = blob.GetSharedAccessSignature(policy);
            log.LogInformation($"Authorized access to receipt \"{request.Query["name"]}\" for user {B2cHelper.GetOid(principal)}");
            var authorization = new ReceiptAuthorizationDto { Url = $"{blob.Uri}{sas}" };
            return new OkObjectResult(authorization);
        }
    }
}