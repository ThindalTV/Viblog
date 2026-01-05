using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Viblog.Components;
using Viblog.Shared.Data;
using Viblog.Frontend;
using Viblog.Api;
using Viblog.Shared;
using Viblog.Shared.Configuration;
using Viblog.Admin;
using Viblog.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure all Viblog settings using the IOptions pattern
builder.Services.AddViblogConfiguration(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure CosmosDB
var cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosConnection")
    ?? throw new InvalidOperationException("Connection string 'CosmosConnection' not found.");
var cosmosDatabaseName = builder.Configuration["CosmosDb:DatabaseName"]
    ?? throw new InvalidOperationException("CosmosDb:DatabaseName configuration not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseCosmos(
        cosmosConnectionString,
        cosmosDatabaseName,
        cosmosOptions =>
        {
            // In development, configure for the emulator
            if (builder.Environment.IsDevelopment())
            {
                // Use Gateway mode for the emulator (required for localhost)
                cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);

                // Limit to endpoint to prevent DNS resolution to internal Docker IPs
                cosmosOptions.LimitToEndpoint();

                // Accept self-signed certificates from the emulator
                cosmosOptions.HttpClientFactory(() => new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }));
            }
            else
            {
                // Use Direct mode for production (better performance)
                cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Direct);
            }
        });
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Register repositories
builder.Services.AddRepositories();

// Register blog services
builder.Services.AddBlogServices();

// Register media storage services
builder.Services.AddMediaStorage(builder.Configuration);

// Register Viblog Frontend services (statically rendered, no auth)
builder.Services.AddViblogFrontend();

// Register Viblog Admin services (interactive server, with auth)
builder.Services.AddViblogAdmin();

var app = builder.Build();

// Ensure database and containers are created
await EnsureDatabaseCreatedAsync(app);

// Seed database with sample data if empty
await SeedDatabaseAsync(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Configure static file serving for media files
var mediaBasePath = builder.Configuration["MediaStorage:FileSystem:BasePath"];
if (!string.IsNullOrEmpty(mediaBasePath) && Directory.Exists(mediaBasePath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(mediaBasePath),
        RequestPath = "/media"
    });
}

app.UseViblogFrontend();
app.UseViblogAdmin();

app.UseAntiforgery();

app.MapStaticAssets();

// Map admin endpoints
app.MapViblogAdminEndpoints();

// Map all blog API endpoints
app.MapViblogApiEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>
/// Ensure the CosmosDB database and containers are created
/// </summary>
static async Task EnsureDatabaseCreatedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Ensuring CosmosDB database and containers are created...");
        await dbContext.Database.EnsureCreatedAsync();
        logger.LogInformation("CosmosDB database and containers are ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while ensuring the database was created.");
        throw;
    }
}

/// <summary>
/// Seed the database with sample data if empty
/// </summary>
static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Checking if database seeding is needed...");
    await Viblog.Shared.Data.Seeders.BlogPostSeeder.SeedAsync(dbContext);
    logger.LogInformation("Database seeding completed.");

}
