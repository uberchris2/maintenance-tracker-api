using Azure.Storage.Blobs;
using maintenance_tracker_api.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddTransient<IB2cHelper, B2cHelper>();

        services.AddSingleton(_ =>
            new CosmosClient(context.Configuration["CosmosDBConnection"]));

        services.AddSingleton(sp =>
            sp.GetRequiredService<CosmosClient>().GetDatabase("MaintenanceDB").GetContainer("VehicleMaintenance"));

        services.AddSingleton(_ =>
            new BlobContainerClient(context.Configuration["UploadStorage"], "receipts"));
    })
    .Build();

await host.RunAsync();
