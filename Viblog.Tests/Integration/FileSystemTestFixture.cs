using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Viblog.Admin.Services.Authentication;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Tests.Integration;

/// <summary>
/// Test fixture that provides isolated InMemory database test environment
/// Each test class gets a fresh database
/// NOTE: This will be removed in Step 6 of Auth0 migration
/// </summary>
public class FileSystemTestFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private bool _disposed;

    public IUserManagementService UserManagementService { get; }
    public TestDbContext DbContext { get; }

    public FileSystemTestFixture()
    {
        var services = new ServiceCollection();

        // Add InMemory database for testing
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add services (temporarily - will be removed in Step 6)
        // services.AddScoped<IUserManagementService, UserManagementService>();

        _serviceProvider = services.BuildServiceProvider();

        // Get services from DI
        // UserManagementService = _serviceProvider.GetRequiredService<IUserManagementService>();
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
    /// Simple DbContext for testing - includes AdminUser
    /// </summary>
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<AdminUser> Users => Set<AdminUser>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure AdminUser with custom properties
            builder.Entity<AdminUser>(b =>
            {
                b.Property(u => u.CustomClaims);
            });
        }
    }
}