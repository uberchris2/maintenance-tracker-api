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
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;

namespace maintenance_tracker_api
{
    public class Functions
    {
        private readonly IB2cHelper _b2cHelper;
        private readonly IMapper _mapper;

        public Functions(IB2cHelper b2cHelper)
        {
            _b2cHelper = b2cHelper;
            _mapper = Mapper.Instance; //todo DI this
        }

        [FunctionName("VehiclesPost")]
        public void VehiclesPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "vehicles")] VehicleDto request,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "VehicleMaintenance",
                ConnectionStringSetting = "CosmosDBConnection")]out VehicleMaintenance vehicle,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            vehicle = _mapper.Map<VehicleMaintenance>(request);
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
            var vehicles = client.CreateDocumentQuery<VehicleMaintenance>(uri)
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
            var options = new RequestOptions {PartitionKey = new PartitionKey(_b2cHelper.GetOid(principal).ToString())};
            var documentResponse = await client.ReadDocumentAsync<VehicleMaintenance>(uri, options, token);
            var vehicle = _mapper.Map<VehicleDto>(documentResponse.Document);
            log.LogInformation($"Got vehicle id {id} for user {_b2cHelper.GetOid(principal)}");
            return new OkObjectResult(vehicle);
        }

        [FunctionName("VehicleMaintenanceGet")]
        public IActionResult VehicleMaintenanceGet(
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
                .Where(x => x.UserId == _b2cHelper.GetOid(principal) && (x.id == parsedId || x.VehicleId == parsedId)).ToList();
            var vehicle = _mapper.Map<VehicleMaintenanceDto>(vehiclesAndMaintenance.Single(vm => vm.Type == VehicleMaintenanceTypes.Vehicle));
            vehicle.Maintenance = _mapper
                .Map<IEnumerable<MaintenanceDto>>(vehiclesAndMaintenance.Where(vm => vm.Type == VehicleMaintenanceTypes.Maintenance))
                .OrderByDescending(m => m.Date);
            log.LogInformation($"Got all vehicles for user {_b2cHelper.GetOid(principal)}");
            return new OkObjectResult(vehicle);
        }

        [FunctionName("MaintenancePost")]
        public void MaintenancePost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "maintenance")] MaintenanceDto request,
            [CosmosDB(
                databaseName: "MaintenanceDB",
                collectionName: "VehicleMaintenance",
                ConnectionStringSetting = "CosmosDBConnection")]out VehicleMaintenance maintenance,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            maintenance = _mapper.Map<VehicleMaintenance>(request); ;
            maintenance.id = Guid.NewGuid();
            maintenance.UserId = _b2cHelper.GetOid(principal);
            maintenance.Type = VehicleMaintenanceTypes.Maintenance;
            log.LogInformation($"Saving new maintenance id {maintenance.id} for user {_b2cHelper.GetOid(principal)}");
        }

        [FunctionName("VehicleMaintenanceDelete")]
        public async Task VehicleMaintenanceDelete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "vehicleMaintenance/{id}")] HttpRequest request,
            string id,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal principal,
            CancellationToken token
        )
        {
            var uri = UriFactory.CreateDocumentUri("MaintenanceDB", "VehicleMaintenance", id);
            var options = new RequestOptions { PartitionKey = new PartitionKey(_b2cHelper.GetOid(principal).ToString()) };
            await client.DeleteDocumentAsync(uri, options, token);
            log.LogInformation($"Deleted maintenance id {id} for user {_b2cHelper.GetOid(principal)}");
        }

        [FunctionName("ReceiptAuthorizationGet")]
        public IActionResult ReceiptAuthorizationGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authorizeReceipt")] HttpRequest request,
            [Blob("receipts", FileAccess.ReadWrite, Connection = "UploadStorage")] CloudBlobContainer container,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            var blob = container.GetBlockBlobReference($"{_b2cHelper.GetOid(principal)}/{request.Query["name"]}");
            var policy = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
            };
            var sas = blob.GetSharedAccessSignature(policy);
            log.LogInformation($"Authorized access to receipt \"{request.Query["name"]}\" for user {_b2cHelper.GetOid(principal)}");
            var authorization = new ReceiptAuthorizationDto { Url = $"{blob.Uri}{sas}" };
            return new OkObjectResult(authorization);
        }

        [FunctionName("FeedbackPost")]
        public void FeedbackPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "feedback")] FeedbackDto request,
            [SendGrid] out SendGridMessage message,
            ILogger log,
            ClaimsPrincipal principal
        )
        {
            message = new SendGridMessage();
            message.AddTo(Environment.GetEnvironmentVariable("FeedbackRecipient"));
            message.AddContent("text/html", $"{_b2cHelper.GetName(principal)} says:<br /><br />{request.Message}");
            message.SetFrom(_b2cHelper.GetEmail(principal));
            message.SetSubject("Feedback from MaintenanceTracker");
            log.LogInformation($"Sending feedback message from user {_b2cHelper.GetOid(principal)}");
        }
    }
}