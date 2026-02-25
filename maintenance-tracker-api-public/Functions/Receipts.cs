using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace maintenance_tracker_api_public.Functions
{
    public class Receipts
    {
        private readonly CosmosClient _cosmosClient;
        private readonly BlobContainerClient _container;
        private readonly ILogger<Receipts> _logger;

        public Receipts(CosmosClient cosmosClient, BlobContainerClient container, ILogger<Receipts> logger)
        {
            _cosmosClient = cosmosClient;
            _container = container;
            _logger = logger;
        }

        [Function("ReceiptAuthorizationGet")]
        public async Task<IActionResult> ReceiptAuthorizationGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authorizeReceipt")] HttpRequest request)
        {
            var vehicleId = Guid.Parse(request.Query["vehicleId"]!);
            var userId = Guid.Parse(request.Query["userId"]!);
            var name = request.Query["name"].ToString();

            var query = _cosmosClient.GetDatabase("MaintenanceDB").GetContainer("VehicleMaintenance")
                .GetItemLinqQueryable<VehicleMaintenanceModel>()
                .Where(x => x.UserId == userId && (x.id == vehicleId || x.VehicleId == vehicleId))
                .ToFeedIterator();

            var vehiclesAndMaintenance = new List<VehicleMaintenanceModel>();
            while (query.HasMoreResults)
                vehiclesAndMaintenance.AddRange(await query.ReadNextAsync(request.HttpContext.RequestAborted));

            if (!vehiclesAndMaintenance.Single(vm => vm.Type == VehicleMaintenanceTypes.Vehicle).Shared
                || !vehiclesAndMaintenance.Any(vm => vm.Type == VehicleMaintenanceTypes.Maintenance && vm.Receipt == name))
            {
                return new BadRequestResult();
            }

            var blobClient = _container.GetBlobClient($"{userId}/{name}");
            var sasBuilder = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1))
            {
                BlobContainerName = _container.Name,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5)
            };
            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            _logger.LogInformation($"Authorized access to receipt \"{name}\" for anonymous user at {request.HttpContext.Connection.RemoteIpAddress}");
            return new OkObjectResult(new ReceiptAuthorizationDto { Url = sasUri.ToString() });
        }
    }
}
