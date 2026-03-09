using Projects;

var builder = DistributedApplication.CreateBuilder(args);


#pragma warning disable ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var cosmos = builder.AddAzureCosmosDB("cosmosserverthingy")
    .RunAsPreviewEmulator(c =>
    {
        c.WithLifetime(ContainerLifetime.Persistent)
        //.WithDataVolume()
        
        .WithDataExplorer(1234)
        //.WithHttpsEndpoint(8081, 8081)
        .WithExternalHttpEndpoints();
    }
    );
#pragma warning restore ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var cosmosStorage = cosmos
    .AddCosmosDatabase("aspireCosmosDatabase");
    /*.RunAsEmulator(conf =>
    {
        conf
        .WithImageTag("linux/latest");
    })
    .AddCosmosDatabase("ViblogDb")*/

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(c =>
    {
        c.WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume();
    });

var blobStorage = storage.AddBlobs("blogStorage");

builder.AddProject<Viblog>("Viblog")
    .WithReference(cosmosStorage)
    .WithReference(blobStorage)
    .WaitFor(cosmos)
    .WaitFor(blobStorage);

builder.AddProject<EricJohansson_se>("EricJohansson")
    .WithReference(cosmosStorage)
    .WithReference(blobStorage)
    .WaitFor(cosmos)
    .WaitFor(blobStorage);


builder.Build().Run();
