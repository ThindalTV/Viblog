# CosmosDB Emulator Configuration for Development

## Overview
This document describes the configuration for using the Azure Cosmos DB Linux emulator in Docker during local development.

## Docker Container

### Running the Emulator
```bash
docker run -d \
  -p 8081:8081 \
  -p 10251:10251 \
  -p 10252:10252 \
  -p 10253:10253 \
  -p 10254:10254 \
  --name cosmosdb-emulator \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

### Port Mappings
- `8081` - HTTPS endpoint (Data Explorer)
- `10251-10254` - Additional Cosmos DB emulator ports

### Verify Container
```bash
docker ps
```

Should show: `0.0.0.0:8081->8081/tcp, 0.0.0.0:10251-10254->10251-10254/tcp`

## Application Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "CosmosConnection": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  },
  "CosmosDb": {
    "DatabaseName": "ViblogDb"
  }
}
```

**Note**: The AccountKey is the standard emulator key - same for all installations.

### Program.cs Configuration

```csharp
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
                    ServerCertificateCustomValidationCallback = 
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }));
            }
            else
            {
                // Use Direct mode for production (better performance)
                cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Direct);
            }
        });
});
```

**Key Configuration Options:**

1. **ConnectionMode.Gateway** - Required for Docker emulator
   - Uses HTTP/HTTPS for all communication
   - Works with localhost addresses
   - Prevents internal Docker IP resolution

2. **LimitToEndpoint()** - Critical for emulator
   - Prevents DNS resolution of gateway addresses
   - Forces all connections through the configured endpoint
   - Without this, CosmosDB tries to connect to 172.17.0.2 (internal Docker IP)

3. **HttpClientFactory** - SSL certificate bypass
   - Accepts self-signed certificate from emulator
   - Only enabled in Development environment
   - Not used in production

## SSL Certificate Handling

### The Problem
The Linux Docker emulator uses a self-signed SSL certificate that .NET applications don't trust by default.

### The Solution
Two options:

#### Option 1: Bypass Certificate Validation (Development Only) ? Current
Configure the HttpClient to accept any certificate in development mode.

**Pros:**
- Simple configuration
- No manual certificate installation
- Works immediately

**Cons:**
- Only suitable for development
- Must be environment-gated

#### Option 2: Install Emulator Certificate
Download and install the emulator's certificate to the OS trust store.

**Download Certificate:**
```powershell
# Windows
$parameters = @{
    Uri = 'https://localhost:8081/_explorer/emulator.pem'
    Method = 'GET'
    OutFile = 'emulatorcert.crt'
    SkipCertificateCheck = $True
}
Invoke-WebRequest @parameters
```

```bash
# Linux
curl --insecure https://localhost:8081/_explorer/emulator.pem > ~/emulatorcert.crt
```

**Install Certificate:**
```powershell
# Windows
$parameters = @{
    FilePath = 'emulatorcert.crt'
    CertStoreLocation = 'Cert:\CurrentUser\Root'
}
Import-Certificate @parameters
```

```bash
# Linux (Debian/Ubuntu)
sudo cp ~/emulatorcert.crt /usr/local/share/ca-certificates/
sudo update-ca-certificates
```

## Accessing the Data Explorer

Open in your browser:
```
https://localhost:8081/_explorer/index.html
```

Your browser will warn about the self-signed certificate - this is expected and safe for local development.

## Database Initialization

### Automatic Initialization (Configured ?)
The application is configured to automatically create the database and containers on startup in `Program.cs`:

```csharp
// Ensure database and containers are created
await EnsureDatabaseCreatedAsync(app);

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
```

**What This Does:**
- Creates the database if it doesn't exist
- Creates all configured containers if they don't exist
- Logs the initialization process
- Throws an exception if initialization fails (app won't start)

**When It Runs:**
- On every application startup
- Safe to call multiple times (idempotent)
- Only creates missing resources

### Manual Initialization
You can also create the database manually using the CosmosDB Data Explorer at:
```
https://localhost:8081/_explorer/index.html
```

## Troubleshooting

### SSL Certificate Error
**Error:** "The SSL connection could not be established"

**Solution:** Verify the HttpClientFactory configuration is in place and the environment is Development.

### Connection Refused
**Error:** "No connection could be made because the target machine actively refused it"

**Solutions:**
1. Verify Docker container is running: `docker ps`
2. Check port 8081 is exposed
3. Verify connection string uses `https://localhost:8081/`

### Connecting to Internal Docker IP (172.17.0.2)
**Error:** "Unable to connect to 172.17.0.2" or timeout errors

**Root Cause:** CosmosDB client in Direct mode resolves gateway addresses to internal Docker IPs

**Solution:** Use Gateway mode with `LimitToEndpoint()`:
```csharp
cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
cosmosOptions.LimitToEndpoint();
```

**Why This Happens:**
- Docker emulator returns internal container IP in gateway addresses
- Direct mode tries to connect to these internal IPs
- Your application can't reach internal Docker network IPs
- Gateway mode + LimitToEndpoint forces use of localhost

**Verification:**
- Connection string uses `https://localhost:8081/`
- Gateway mode is configured
- `LimitToEndpoint()` is called
- Application connects successfully

### Database Not Created
**Error:** Database or container doesn't exist

**Solutions:**
1. Enable EF Core logging to see what's happening
2. Add `EnsureCreatedAsync()` call
3. Check Data Explorer to verify database state

## Environment-Specific Configuration

### Development (Current Setup)
- Uses emulator at `localhost:8081`
- **Gateway connection mode** (required for emulator)
- **LimitToEndpoint()** enabled (prevents internal IP resolution)
- Bypasses SSL certificate validation
- Standard emulator AccountKey

### Production
- Uses Azure Cosmos DB in the cloud
- **Direct connection mode** (better performance)
- Real SSL certificates (no bypass needed)
- Secure AccountKey from Azure
- No endpoint limitation (uses optimal routing)

### Connection Mode Comparison

| Feature | Gateway Mode | Direct Mode |
|---------|--------------|-------------|
| **Protocol** | HTTPS | TCP |
| **Performance** | Good | Excellent |
| **Latency** | Higher | Lower |
| **Firewall** | Easy (port 443/8081) | Complex (multiple ports) |
| **Docker Emulator** | ? Works | ? Internal IP issues |
| **Production** | ? Works | ? Recommended |
| **Use Case** | Development, firewalls | Production, best performance |

**Why Gateway Mode for Emulator?**
- Emulator returns internal Docker IPs in Direct mode
- Application can't reach 172.17.0.x addresses
- Gateway mode uses only the configured endpoint
- `LimitToEndpoint()` prevents DNS resolution

**Why Direct Mode for Production?**
- Better performance (TCP vs HTTPS)
- Lower latency
- More efficient resource usage
- Azure CosmosDB optimized for Direct mode

### Configuration Example

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "CosmosConnection": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  },
  "CosmosDb": {
    "DatabaseName": "ViblogDb"
  }
}
```

**appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "CosmosConnection": "AccountEndpoint=https://your-cosmos-account.documents.azure.com:443/;AccountKey=YOUR-PRODUCTION-KEY-HERE"
  },
  "CosmosDb": {
    "DatabaseName": "ViblogDb"
  }
}
```

## Security Notes

### ?? Important Security Considerations

1. **Never bypass certificate validation in production**
   - The `DangerousAcceptAnyServerCertificateValidator` is only for development
   - Always gate it with `if (builder.Environment.IsDevelopment())`

2. **Emulator AccountKey is public**
   - The emulator key is the same for everyone
   - Never use it in production
   - It's safe for local development only

3. **Production Connection Strings**
   - Store production keys in Azure Key Vault
   - Use User Secrets for local production testing
   - Never commit production keys to source control

## Testing the Connection

### Simple Test
1. Start the Docker container
2. Run your application
3. Try to save/retrieve data
4. Check Data Explorer at `https://localhost:8081/_explorer/index.html`

### Verify Configuration
```csharp
// In a controller or page
public async Task<IActionResult> TestConnection()
{
    try
    {
        // Try to ensure database exists
        await _dbContext.Database.EnsureCreatedAsync();
        return Ok("Connection successful!");
    }
    catch (Exception ex)
    {
        return BadRequest($"Connection failed: {ex.Message}");
    }
}
```

## Additional Resources

- [Azure Cosmos DB Emulator Documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator)
- [EF Core Cosmos Provider](https://learn.microsoft.com/en-us/ef/core/providers/cosmos/)
- [Docker Hub - Cosmos DB Emulator](https://hub.docker.com/r/microsoft/azure-cosmosdb-emulator)
