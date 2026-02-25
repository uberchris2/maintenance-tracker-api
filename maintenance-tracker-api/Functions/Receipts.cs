using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
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
    public class Receipts
    {
        private readonly IB2cHelper _b2cHelper;
        private readonly CosmosClient _cosmosClient;
        private readonly BlobContainerClient _container;
        private readonly ILogger<Receipts> _logger;

        public Receipts(IB2cHelper b2cHelper, CosmosClient cosmosClient, BlobContainerClient container, ILogger<Receipts> logger)
        {
            _b2cHelper = b2cHelper;
            _cosmosClient = cosmosClient;
            _container = container;
            _logger = logger;
        }

        [Function("ReceiptAuthorizationGet")]
        public IActionResult ReceiptAuthorizationGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authorizeReceipt")] HttpRequest request)
        {
            var userId = _b2cHelper.GetOid(request);
            var name = request.Query["name"].ToString();
            var blobClient = _container.GetBlobClient($"{userId}/{name}");
            var sasBuilder = new BlobSasBuilder(BlobSasPermissions.Read | BlobSasPermissions.Write, DateTimeOffset.UtcNow.AddHours(1))
            {
                BlobContainerName = _container.Name,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5)
            };
            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            _logger.LogInformation($"Authorized access to receipt \"{name}\" for user {userId}");
            return new OkObjectResult(new ReceiptAuthorizationDto { Url = sasUri.ToString() });
        }

        [Function("ReceiptsGet")]
        public async Task<IActionResult> ReceiptsGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "receipts")] HttpRequest request)
        {
            var userId = _b2cHelper.GetOid(request);
            var query = _cosmosClient.GetDatabase("MaintenanceDB").GetContainer("VehicleMaintenance")
                .GetItemLinqQueryable<MaintenanceModel>(
                    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId.ToString()) })
                .Where(x => x.UserId == userId && x.Type == VehicleMaintenanceTypes.Maintenance && x.Receipt != null)
                .ToFeedIterator();

            var receipts = new List<string>();
            while (query.HasMoreResults)
            {
                foreach (var item in await query.ReadNextAsync(request.HttpContext.RequestAborted))
                    receipts.Add(item.Receipt);
            }

            _logger.LogInformation($"Got receipts for user {userId}");
            return new OkObjectResult(receipts.Distinct());
        }
    }
}
