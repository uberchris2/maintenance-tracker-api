using Azure.Storage.Blobs;
using common;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SendGrid;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddAutoMapper(typeof(MappingProfile));

        services.AddSingleton(_ =>
            new CosmosClient(context.Configuration["CosmosDBConnection"]));

        services.AddSingleton(_ =>
            new BlobContainerClient(context.Configuration["UploadStorage"], "receipts"));

        services.AddSingleton<ISendGridClient>(_ =>
            new SendGridClient(context.Configuration["AzureWebJobsSendGridApiKey"]));
    })
    .Build();

await host.RunAsync();
