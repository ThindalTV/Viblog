using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viblog.Data.CosmosDb.Data.Repositories;
using Viblog.Infrastructure.Data.Entities;
using Viblog.Shared.Data.Sources.CosmosDb.Data;

namespace Viblog.Tests.Data.Repositories;

/// <summary>
/// Tests for CosmosDbMediaMetadataRepository.
/// These tests document the concurrent DbContext access bug introduced by
/// CosmosDbRepository's constructor calling EnsureCreatedAsync() without awaiting it.
/// </summary>
public class CosmosDbMediaMetadataRepositoryTests
{
    // -------------------------------------------------------------------------
    // Test infrastructure
    // -------------------------------------------------------------------------

    /// <summary>
    /// ApplicationDbContext subclass with an InMemory-compatible model.
    /// The production context uses Cosmos-specific APIs (ToContainer, HasPartitionKey)
    /// and serialises Dictionary&lt;string,string&gt; as JSON — neither is supported by the
    /// InMemory provider.  This subclass overrides OnModelCreating to provide simple,
    /// InMemory-compatible mappings for the entities used in these tests.
    /// </summary>
    private sealed class TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MediaItem>(b =>
            {
                b.HasKey(m => m.Id);
                // Serialise the dictionary as a JSON string — Cosmos does the same
                // natively; for InMemory we need an explicit value converter.
                b.Property(m => m.AdditionalMetadata)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
                             ?? new Dictionary<string, string>());
            });
        }
    }

    private static TestApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestApplicationDbContext(options);
    }

    private static MediaItem MakeItem(string id, string storagePath, string mimeType = "image/jpeg")
        => new()
        {
            Id = id,
            GroupKey = mimeType.Split('/')[0],
            FileName = Path.GetFileName(storagePath),
            FileExtension = Path.GetExtension(storagePath),
            MimeType = mimeType,
            StoragePath = storagePath,
        };

    // -------------------------------------------------------------------------
    // Bug-documentation test
    //
    // Demonstrates the EF Core constraint that caused the production crash.
    // EF Core forbids concurrent operations on the same DbContext instance.
    // CosmosDbRepository's constructor used to call EnsureCreatedAsync() without
    // awaiting it, which — with real Cosmos DB (50-500 ms network latency) —
    // left an operation in-flight on the context.  Any subsequent repository
    // method call then triggered the "second operation" InvalidOperationException.
    //
    // This test uses two thread-pool threads racing to enter EF Core's critical
    // section simultaneously to prove the same invariant holds.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DbContext_ConcurrentOperationsFromDifferentThreads_ThrowsInvalidOperationException()
    {
        await using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Seed a few items so the query actually touches data
        context.Set<MediaItem>().AddRange(
            Enumerable.Range(1, 5).Select(i => MakeItem($"{i}", $"images/2024/06/photo{i}.jpg")));
        await context.SaveChangesAsync();

        var barrier = new Barrier(2);

        // Two tasks released simultaneously from the barrier so they race to enter
        // EF Core's per-context critical section at the same instant.
        // Interlocked.CompareExchange inside ConcurrencyDetector ensures exactly
        // one succeeds; the other throws InvalidOperationException.
        var task1 = Task.Run(async () => { barrier.SignalAndWait(); return await context.Set<MediaItem>().ToListAsync(); });
        var task2 = Task.Run(async () => { barrier.SignalAndWait(); return await context.Set<MediaItem>().ToListAsync(); });

        // At least one of the two tasks must throw the concurrent-access exception.
        // (If the InMemory provider completes one task before the other thread even
        // enters — an extremely rare timing scenario — Record.ExceptionAsync will
        // return null and the assertion below will call this out clearly.)
        var ex = await Record.ExceptionAsync(() => Task.WhenAll(task1, task2));

        Assert.True(
            ex is InvalidOperationException || task1.IsFaulted || task2.IsFaulted,
            "Expected at least one task to fail with InvalidOperationException due to " +
            "concurrent DbContext access, but both tasks succeeded.  This can happen " +
            "when the InMemory provider completes operations faster than the OS " +
            "scheduler can interleave two threads.  The bug is reliably triggered in " +
            "production where Cosmos DB EnsureCreatedAsync takes 50-500 ms.");
    }

    // -------------------------------------------------------------------------
    // Regression tests — these verify expected behaviour after the fix.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetDateFoldersAsync_WithNoData_ReturnsEmptyList()
    {
        await using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        var repository = new CosmosDbMediaMetadataRepository(context);
        var result = await repository.GetDateFoldersAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDateFoldersAsync_WithMediaItems_ReturnsDistinctDateFoldersNewestFirst()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        context.Set<MediaItem>().AddRange(
            MakeItem("1", "images/2024/06/photo.jpg"),
            MakeItem("2", "images/2024/06/photo2.jpg"),  // same month as id=1 — deduped
            MakeItem("3", "documents/2024/07/doc.pdf", "application/pdf")
        );
        await context.SaveChangesAsync();

        var repository = new CosmosDbMediaMetadataRepository(context);

        // Act
        var result = await repository.GetDateFoldersAsync();

        // Assert: two distinct months, newest first
        Assert.Equal(2, result.Count);
        Assert.Equal("202407", result[0]);
        Assert.Equal("202406", result[1]);
    }

    [Fact]
    public async Task GetDateFoldersAsync_WithDeletedItems_ExcludesDeletedItems()
    {
        await using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        var item = MakeItem("1", "images/2024/06/photo.jpg");
        item.IsDeleted = true;
        context.Set<MediaItem>().Add(item);
        await context.SaveChangesAsync();

        var repository = new CosmosDbMediaMetadataRepository(context);
        var result = await repository.GetDateFoldersAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDateFoldersAsync_CalledSequentiallyOnSameContext_DoesNotThrow()
    {
        // Verifies that sequential (non-concurrent) calls on the same context work
        // correctly — the baseline expectation after the constructor fix.
        await using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        var repository = new CosmosDbMediaMetadataRepository(context);

        var first = await repository.GetDateFoldersAsync();
        var second = await repository.GetDateFoldersAsync();

        Assert.Equal(first, second);
    }
}

