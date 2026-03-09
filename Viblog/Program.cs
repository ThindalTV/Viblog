using Viblog.Components;
using Viblog.Shared;
using Viblog.Shared.Configuration;
using Viblog.Admin;
using Viblog.Shared.Data.Sources.CosmosDb.Data;
using Viblog.Shared.Data.Sources.CosmosDb;
using Viblog.Shared.Data.Sources.AzureStorage;

var builder = WebApplication.CreateBuilder(args);

// Aspire
builder.AddServiceDefaults();

// Configure all Viblog settings using the IOptions pattern
builder.Services.AddViblogConfiguration(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Circuit options for Blazor Server
builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.DetailedErrors = true;
    }
});

builder.AddCosmosDbContext<ApplicationDbContext>("aspireCosmosDatabase");

// Configure Filesystem Data Access (replacing CosmosDB)
builder.Services.AddCosmosDbContext(builder.Configuration, false);
builder.Services.AddCosmosDbRepositories();
// builder.Services.AddFilesystemDataAccess(builder.Configuration);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Register blog services
builder.Services.AddBlogServices();

// Register media storage services
builder.Services.AddAzureBlobStorageRepository();

// Register Viblog Admin services (interactive server, with auth)
builder.Services.AddViblogAdmin();

var app = builder.Build();



// Initialize admin system (creates default admin user in Auth0 if needed)
await app.InitializeViblogAdminAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Seed database with sample data if empty
    await SeedDatabaseAsync(app);
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
if (!string.IsNullOrEmpty(mediaBasePath))
{
    // Ensure the path is absolute
    var absoluteMediaPath = Path.IsPathRooted(mediaBasePath) 
        ? mediaBasePath 
        : Path.GetFullPath(mediaBasePath);

    if (Directory.Exists(absoluteMediaPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(absoluteMediaPath),
            RequestPath = "/media"
        });
    }
    else
    {
        app.Logger.LogWarning("Media storage path does not exist: {Path}. Static file serving for media will not be configured.", absoluteMediaPath);
    }
}

// Authentication & Authorization middleware
// Must be after UseRouting() and before MapRazorComponents()
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// Map admin endpoints
app.MapViblogAdminEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>
/// Seed the database with sample data if empty
/// </summary>
static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    // Ensure the Cosmos DB database and all containers exist before any
    // repository is used. This replaces the fire-and-forget call that was
    // previously in CosmosDbRepository's constructor.
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<Viblog.Shared.Data.Seeders.BlogPostSeeder>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking if database seeding is needed...");
        await seeder.SeedAsync();
        logger.LogInformation("Database seeding check completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database seeding");
        // Don't throw - allow app to start even if seeding fails
    }
}
