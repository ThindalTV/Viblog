using Microsoft.EntityFrameworkCore;
using Viblog.Components;
using Viblog.Data.Filesystem;
using Viblog.Frontend;
using Viblog.Api;
using Viblog.Shared;
using Viblog.Shared.Configuration;
using Viblog.Admin;
using Viblog.Shared.Extensions;
using Microsoft.Extensions.Options;
using Viblog.Data.CosmosDb;
using Viblog.Data.AzureStorage;
using Viblog.Data.CosmosDb.Data;
using Viblog.Infrastructure.Shared.Data.Entities;

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

// Register Viblog Frontend services (statically rendered, no auth)
builder.Services.AddViblogFrontend();

// Register Viblog Admin services (interactive server, with auth)
builder.Services.AddViblogAdmin();

var app = builder.Build();

// Seed database with sample data if empty
await SeedDatabaseAsync(app);

// Initialize admin system (creates default admin user in Auth0 if needed)
await app.InitializeViblogAdminAsync();

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

app.UseViblogFrontend();

// Authentication & Authorization middleware
// Must be after UseRouting() and before MapRazorComponents()
app.UseAuthentication();
app.UseAuthorization();

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
/// Seed the database with sample data if empty
/// </summary>
static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var blogPostRepository = scope.ServiceProvider.GetRequiredService<Viblog.Infrastructure.Shared.Data.Repositories.IBlogPostRepository>();
    var filesystemOptions = scope.ServiceProvider.GetRequiredService<IOptions<Viblog.Data.Filesystem.Configuration.FilesystemStorageOptions>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Checking if database seeding is needed...");
    await Viblog.Data.Filesystem.Data.Seeders.BlogPostSeeder.SeedAsync(
        blogPostRepository, 
        logger, 
        filesystemOptions);
    logger.LogInformation("Database seeding check completed.");
}

/// <summary>
/// Initialize default admin user if no users exist
/// </summary>
static async Task InitializeDefaultAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var userManagementService = scope.ServiceProvider.GetRequiredService<Viblog.Infrastructure.Shared.Authentication.IUserManagementService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking if default admin user initialization is needed...");

        var usersExist = await userManagementService.AnyUsersExistAsync();

        if (!usersExist)
        {
            logger.LogInformation("No users found. Creating default admin user...");
            var defaultAdmin = await userManagementService.CreateDefaultAdminUserAsync();
            logger.LogWarning("Default admin user created: {Email} with password 'admin123!' - CHANGE THIS PASSWORD IMMEDIATELY!", defaultAdmin.Email);
        }
        else
        {
            logger.LogInformation("Users already exist. Skipping default admin creation.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during default admin user initialization");
        throw;
    }
}
