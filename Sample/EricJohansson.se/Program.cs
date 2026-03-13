using EricJohansson.se;
using EricJohansson.se.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Viblog;
using Viblog.Infrastructure.Shared.Services;
using Viblog.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddViblogServices();
builder.UseViblog();

builder.Services.AddViblogFrontend();
builder.Services.AddScoped<ISitemapService, SitemapService>();

builder.Services.Configure<CircuitOptions>(options =>
{
    if (builder.Environment.IsDevelopment())
        options.DetailedErrors = true;
});

var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

await app.UseViblogAsync();

app.MapViblogApiEndpoints();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ViblogStartupExtensions).Assembly);


app.Run();
