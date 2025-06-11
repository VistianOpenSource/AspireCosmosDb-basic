var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECOSMOSDB001

var cosmos = builder.AddAzureCosmosDB("cosmosdb").RunAsPreviewEmulator(
                     emulator =>
                     {
                         emulator.WithDataExplorer();
                     });

var database = cosmos.AddCosmosDatabase("database");
var container = database.AddContainer("container", "/id");



var apiService = builder.AddProject<Projects.AspireCosmosDb_ApiService>("apiservice").
                    WithReference(container);


builder.AddProject<Projects.AspireCosmosDb_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddAzureFunctionsProject<Projects.AspireCosmosDb_Functions>("aspirecosmosdb-functions").
    WithReference(container).
    WithExternalHttpEndpoints();


builder.Build().Run();
