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

namespace maintenance_tracker_api.Functions;

public class Receipts(IB2cHelper b2cHelper, Container cosmosContainer, BlobContainerClient blobContainer, ILogger<Receipts> logger)
{
    [Function("ReceiptAuthorizationGet")]
    public IActionResult ReceiptAuthorizationGet(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authorizeReceipt")] HttpRequest request)
    {
        var userId = b2cHelper.GetOid(request);
        var name = request.Query["name"].ToString();
        var blobClient = blobContainer.GetBlobClient($"{userId}/{name}");
        var sasBuilder = new BlobSasBuilder(BlobSasPermissions.Read | BlobSasPermissions.Write, DateTimeOffset.UtcNow.AddHours(1))
        {
            BlobContainerName = blobContainer.Name,
            BlobName = blobClient.Name,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        logger.LogInformation("Authorized access to receipt {ReceiptName} for user {UserId}", name, userId);
        return new OkObjectResult(new ReceiptAuthorizationDto { Url = sasUri.ToString() });
    }

    [Function("ReceiptsGet")]
    public async Task<IActionResult> ReceiptsGet(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "receipts")] HttpRequest request)
    {
        var userId = b2cHelper.GetOid(request);
        var query = cosmosContainer
            .GetItemLinqQueryable<MaintenanceModel>(
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId.ToString()) })
            .Where(x => x.UserId == userId && x.Type == VehicleMaintenanceTypes.Maintenance && x.Receipt != null)
            .ToFeedIterator();

        var receipts = new List<string>();
        while (query.HasMoreResults)
        {
            foreach (var item in await query.ReadNextAsync(request.HttpContext.RequestAborted))
                receipts.Add(item.Receipt!);
        }

        logger.LogInformation("Got receipts for user {UserId}", userId);
        return new OkObjectResult(receipts.Distinct());
    }
}
