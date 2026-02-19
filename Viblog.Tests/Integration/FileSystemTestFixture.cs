using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Viblog.Admin.Services.Authentication;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Tests.Integration;

/// <summary>
/// Test fixture that provides isolated InMemory database test environment with Identity
/// Each test class gets a fresh database
/// </summary>
public class FileSystemTestFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private bool _disposed;

    public UserManager<ApplicationUser> UserManager { get; }
    public IAuthenticationProvider AuthenticationProvider { get; }
    public IUserManagementService UserManagementService { get; }
    public TestDbContext DbContext { get; }

    public FileSystemTestFixture()
    {
        var services = new ServiceCollection();

        // Add InMemory database for testing
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        // Add Identity with ApplicationUser
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Disable password requirements for easier testing
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 1;
            options.Password.RequiredUniqueChars = 0;
        })
        .AddEntityFrameworkStores<TestDbContext>()
        .AddDefaultTokenProviders();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add services
        services.AddScoped<IAuthenticationProvider, LocalAuthenticationProvider>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        _serviceProvider = services.BuildServiceProvider();

        // Get services from DI
        UserManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        AuthenticationProvider = _serviceProvider.GetRequiredService<IAuthenticationProvider>();
        UserManagementService = _serviceProvider.GetRequiredService<IUserManagementService>();
        DbContext = _serviceProvider.GetRequiredService<TestDbContext>();

        // Ensure database is created
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            DbContext?.Dispose();
            _serviceProvider?.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Simple DbContext for testing - only includes Identity entities
    /// </summary>
    public class TestDbContext : IdentityDbContext<ApplicationUser>
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser with custom properties
            builder.Entity<ApplicationUser>(b =>
            {
                b.Property(u => u.CustomClaims);
            });
        }
    }
}

