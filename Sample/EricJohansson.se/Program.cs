using EricJohansson.se;
using EricJohansson.se.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Viblog;
using Viblog.Infrastructure.Shared.Services;
using Viblog.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register ericjohansson.se services
builder.Services.AddEJFacades();
builder.Services.AddScoped<ISitemapService, SitemapService>();
// Helpers
builder.Services.AddScoped<StructuredDataHelper>();

builder.Services.Configure<CircuitOptions>(options =>
{
    if (builder.Environment.IsDevelopment())
        options.DetailedErrors = true;
});

// Add backend
builder.AddViblog();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ViblogStartupExtensions).Assembly);

// Use backend
await app.UseViblogAsync();

app.Run();
