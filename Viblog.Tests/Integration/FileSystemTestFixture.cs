using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Admin.Services.Authentication;
using Viblog.Data.Filesystem.Configuration;
using Viblog.Data.Filesystem.Data.Repositories;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Tests.Integration;

/// <summary>
/// Test fixture that provides isolated filesystem-based test environment
/// Each test class gets a fresh database in a temporary directory
/// </summary>
public class FileSystemTestFixture : IDisposable
{
    private readonly string _testDataPath;
    private bool _disposed;

    public IUserRepository UserRepository { get; }
    public IAuthenticationProvider AuthenticationProvider { get; }
    public IUserManagementService UserManagementService { get; }

    public FileSystemTestFixture()
    {
        // Create unique temporary directory for this test run
        _testDataPath = Path.Combine(
            Path.GetTempPath(),
            "Viblog.Tests",
            $"Auth_{Guid.NewGuid()}");

        Directory.CreateDirectory(_testDataPath);

        // Create logger factories
        var repositoryLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var authProviderLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var userServiceLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Create options for filesystem storage
        var storageOptions = Options.Create(new FilesystemStorageOptions
        {
            RootPath = _testDataPath
        });

        // Initialize repository with test data path
        UserRepository = new FileSystemUserRepository(
            storageOptions,
            repositoryLoggerFactory.CreateLogger<FilesystemRepository<Viblog.Infrastructure.Shared.Data.Entities.User>>());

        // Initialize authentication provider
        AuthenticationProvider = new LocalAuthenticationProvider(
            UserRepository,
            authProviderLoggerFactory.CreateLogger<LocalAuthenticationProvider>());

        // Initialize user management service
        UserManagementService = new UserManagementService(
            UserRepository,
            AuthenticationProvider,
            userServiceLoggerFactory.CreateLogger<UserManagementService>());
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
            // Clean up test data directory
            try
            {
                if (Directory.Exists(_testDataPath))
                {
                    Directory.Delete(_testDataPath, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }

        _disposed = true;
    }
}
