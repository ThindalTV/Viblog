using Microsoft.EntityFrameworkCore;
using Viblog.Components;
using Viblog.Data.CosmosDb;
using Viblog.Frontend;
using Viblog.Api;
using Viblog.Shared;
using Viblog.Shared.Configuration;
using Viblog.Admin;
using Viblog.Shared.Extensions;
using Viblog.Data.CosmosDb.Data.Seeders;

var builder = WebApplication.CreateBuilder(args);

// Configure all Viblog settings using the IOptions pattern
builder.Services.AddViblogConfiguration(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure CosmosDB with repositories
builder.Services.AddCosmosDbDataAccess(builder.Configuration, builder.Environment.IsDevelopment());

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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
await app.Services.EnsureCosmosDbCreatedAsync();

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
/// Seed the database with sample data if empty
/// </summary>
static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<Viblog.Data.CosmosDb.Data.ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Checking if database seeding is needed...");
    await BlogPostSeeder.SeedAsync(dbContext);
    logger.LogInformation("Database seeding completed.");
}
