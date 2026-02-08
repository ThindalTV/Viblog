using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viblog.Data.Filesystem.Configuration;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Repositories;

namespace Viblog.Data.Filesystem.Data.Seeders;

/// <summary>
/// Seeds the filesystem storage with initial blog post data
/// </summary>
public static class BlogPostSeeder
{
    /// <summary>
    /// Seed blog posts if the storage is empty
    /// </summary>
    /// <param name="repository">The blog post repository</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="options">Filesystem storage options to check data folder</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task SeedAsync(
        IBlogPostRepository repository,
        ILogger logger,
        IOptions<FilesystemStorageOptions>? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        // If options are provided, check if data folder is empty first
        if (options?.Value != null)
        {
            if (!IsDataFolderEmpty(options.Value, logger))
            {
                logger.LogInformation("Data folder is not empty. Skipping seeding.");
                return;
            }
        }

        // Check if we already have blog posts
        var existingPosts = await repository.GetAllAsync(
            new PagingParameters(1, 1),
            p => p.CreatedAt,
            ascending: false,
            includeDeleted: false,
            cancellationToken);

        if (existingPosts.TotalCount > 0)
        {
            logger.LogInformation("Database already contains data. Skipping seeding.");
            return; // Storage already has data
        }

        logger.LogInformation("Starting database seeding...");

        var now = DateTimeOffset.UtcNow;
        var posts = new List<BlogPost>();

        // Featured Post 1 - Recent
        var post1 = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Getting Started with Blazor Server in .NET 10",
            Slug = "getting-started-blazor-server-net10",
            Short = "Discover the latest features and improvements in Blazor Server for .NET 10, including enhanced performance and new component capabilities.",
            Markdown = "# Getting Started with Blazor Server in .NET 10\n\nBlazor Server has evolved significantly...",
            Content = "<h1>Getting Started with Blazor Server in .NET 10</h1><p>Blazor Server has evolved significantly...</p>",
            FeaturedImageUrl = "/img/blog/blazor-server.jpg",
            FeaturedImageAlt = "Blazor Server architecture diagram",
            AuthorId = "system",
            AuthorName = "System Administrator",
            PublishedAt = now.AddDays(-5),
            IsPublished = true,
            IsFeatured = true,
            ViewCount = 245,
            Tags = ["Blazor", ".NET 10", "Web Development", "Server-Side"],
            CategoryIds = ["blazor", "dotnet"],
            CategoryNames = ["Blazor", ".NET"],
            MetaDescription = "Learn how to get started with Blazor Server in .NET 10",
            ReadingTimeMinutes = 8,
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-5),
            GroupKey = now.AddDays(-5).Year.ToString() // Set partition key
        };

        posts.Add(post1);

        // Featured Post 2 - Recent
        var post2 = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Building Scalable Applications with CosmosDB and EF Core",
            Slug = "building-scalable-apps-cosmosdb-efcore",
            Short = "Learn best practices for designing and implementing scalable cloud applications using Azure CosmosDB with Entity Framework Core.",
            Markdown = "# Building Scalable Applications\n\nCosmosDB offers unparalleled scalability...",
            Content = "<h1>Building Scalable Applications</h1><p>CosmosDB offers unparalleled scalability...</p>",
            FeaturedImageUrl = "/img/blog/cosmosdb.jpg",
            FeaturedImageAlt = "Azure CosmosDB logo",
            AuthorId = "system",
            AuthorName = "System Administrator",
            PublishedAt = now.AddDays(-12),
            IsPublished = true,
            IsFeatured = true,
            ViewCount = 432,
            Tags = ["CosmosDB", "Azure", "Entity Framework", "Scalability"],
            CategoryIds = ["azure", "databases"],
            CategoryNames = ["Azure", "Databases"],
            MetaDescription = "Best practices for building scalable applications with CosmosDB",
            ReadingTimeMinutes = 12,
            CreatedAt = now.AddDays(-12),
            UpdatedAt = now.AddDays(-12),
            GroupKey = now.AddDays(-12).Year.ToString()
        };

        posts.Add(post2);

        // Featured Post 3 - Recent
        var post3 = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Modern CSS Techniques for Responsive Web Design",
            Slug = "modern-css-responsive-design",
            Short = "Explore cutting-edge CSS features including Grid, Flexbox, and Container Queries for creating truly responsive web applications.",
            Markdown = "# Modern CSS Techniques\n\nCSS has come a long way...",
            Content = "<h1>Modern CSS Techniques</h1><p>CSS has come a long way...</p>",
            FeaturedImageUrl = "/img/blog/css-modern.jpg",
            FeaturedImageAlt = "Modern CSS layout examples",
            AuthorId = "system",
            AuthorName = "System Administrator",
            PublishedAt = now.AddDays(-18),
            IsPublished = true,
            IsFeatured = true,
            ViewCount = 389,
            Tags = ["CSS", "Responsive Design", "Web Development", "Frontend"],
            CategoryIds = ["frontend", "css"],
            CategoryNames = ["Frontend", "CSS"],
            MetaDescription = "Learn modern CSS techniques for responsive web design",
            ReadingTimeMinutes = 10,
            CreatedAt = now.AddDays(-18),
            UpdatedAt = now.AddDays(-18),
            GroupKey = now.AddDays(-18).Year.ToString()
        };

        posts.Add(post3);

        // Regular Posts
        posts.AddRange([
            CreateBlogPost("Understanding Dependency Injection in ASP.NET Core", "dependency-injection-aspnet-core",
                "A deep dive into the dependency injection container in ASP.NET Core and how to use it effectively.",
                ["ASP.NET Core", "DI", "Architecture"], ["aspnet", "architecture"],
                now.AddDays(-25), 156, 7),

            CreateBlogPost("Introduction to Docker for .NET Developers", "docker-dotnet-developers",
                "Learn how to containerize your .NET applications using Docker for improved deployment and scalability.",
                ["Docker", ".NET", "DevOps", "Containers"], ["devops", "docker"],
                now.AddDays(-32), 298, 9),

            CreateBlogPost("Async/Await Best Practices in C#", "async-await-best-practices-csharp",
                "Master asynchronous programming in C# with practical examples and common pitfalls to avoid.",
                ["C#", "Async", "Best Practices", "Performance"], ["csharp", "performance"],
                now.AddDays(-38), 521, 11),

            CreateBlogPost("RESTful API Design Principles", "restful-api-design-principles",
                "Essential principles and patterns for designing clean, maintainable RESTful APIs.",
                ["API", "REST", "Design", "Architecture"], ["api", "architecture"],
                now.AddDays(-45), 412, 8),

            CreateBlogPost("Getting Started with Telerik UI for Blazor", "telerik-ui-blazor-getting-started",
                "An introduction to Telerik UI components for Blazor and how to integrate them into your projects.",
                ["Blazor", "Telerik", "UI Components"], ["blazor", "ui"],
                now.AddDays(-52), 234, 6),

            CreateBlogPost("Azure Functions vs AWS Lambda: A Comparison", "azure-functions-vs-aws-lambda",
                "Compare serverless computing platforms and learn which one is right for your project.",
                ["Azure", "AWS", "Serverless", "Cloud"], ["azure", "cloud"],
                now.AddDays(-60), 367, 10),

            CreateBlogPost("SOLID Principles Explained with C# Examples", "solid-principles-csharp-examples",
                "Understanding SOLID principles through practical C# code examples and real-world scenarios.",
                ["C#", "SOLID", "Design Patterns", "Best Practices"], ["csharp", "architecture"],
                now.AddDays(-67), 489, 13),

            CreateBlogPost("Introduction to GraphQL with .NET", "graphql-dotnet-introduction",
                "Learn how to build GraphQL APIs using .NET and Hot Chocolate framework.",
                ["GraphQL", ".NET", "API", "Hot Chocolate"], ["dotnet", "api"],
                now.AddDays(-74), 203, 9),

            CreateBlogPost("Performance Optimization Tips for Blazor Apps", "blazor-performance-optimization",
                "Practical tips and techniques to improve the performance of your Blazor applications.",
                ["Blazor", "Performance", "Optimization"], ["blazor", "performance"],
                now.AddDays(-82), 341, 8),

            CreateBlogPost("Unit Testing Best Practices with xUnit", "unit-testing-xunit-best-practices",
                "Write better unit tests with xUnit, including mocking, test organization, and CI/CD integration.",
                ["Testing", "xUnit", "TDD", "Best Practices"], ["testing", "quality"],
                now.AddDays(-89), 278, 10),

            CreateBlogPost("Securing ASP.NET Core Applications", "securing-aspnet-core-applications",
                "Essential security practices for protecting your ASP.NET Core web applications.",
                ["Security", "ASP.NET Core", "Authentication", "Authorization"], ["security", "aspnet"],
                now.AddDays(-96), 456, 12),

            CreateBlogPost("Exploring Minimal APIs in .NET", "minimal-apis-dotnet-exploration",
                "Discover the simplicity and power of Minimal APIs introduced in recent .NET versions.",
                [".NET", "Minimal APIs", "Web Development"], ["dotnet", "api"],
                now.AddDays(-103), 312, 7)
        ]);

        // Add all posts to repository
        foreach (var post in posts)
        {
            await repository.AddAsync(post, cancellationToken);
        }

        // Save changes (no-op for filesystem but included for consistency)
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Database seeding completed. Added {Count} blog posts.", posts.Count);
    }

    static BlogPost CreateBlogPost(
        string title,
        string slug,
        string shortDescription,
        string[] tags,
        string[] categories,
        DateTimeOffset publishedAt,
        int viewCount,
        int readingTime)
    {
        var categoryNames = categories.Select(c => char.ToUpper(c[0]) + c[1..]).ToList();

        var post = new BlogPost
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Slug = slug,
            Short = shortDescription,
            Markdown = $"# {title}\n\n{shortDescription}\n\nThis is sample content for demonstration purposes.",
            Content = $"<h1>{title}</h1><p>{shortDescription}</p><p>This is sample content for demonstration purposes.</p>",
            AuthorId = "system",
            AuthorName = "System Administrator",
            PublishedAt = publishedAt,
            IsPublished = true,
            IsFeatured = false,
            ViewCount = viewCount,
            Tags = [.. tags],
            CategoryIds = [.. categories],
            CategoryNames = categoryNames,
            MetaDescription = shortDescription,
            ReadingTimeMinutes = readingTime,
            CreatedAt = publishedAt,
            UpdatedAt = publishedAt,
            GroupKey = publishedAt.Year.ToString() // Set partition key based on year
        };

        return post;
    }

    /// <summary>
    /// Check if the data folder is empty by examining the entities directory
    /// </summary>
    /// <param name="options">Filesystem storage options</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>True if the data folder is empty, false otherwise</returns>
    private static bool IsDataFolderEmpty(FilesystemStorageOptions options, ILogger logger)
    {
        try
        {
            var rootPath = Path.GetFullPath(options.RootPath);
            var entitiesPath = Path.Combine(rootPath, options.EntitiesDirectory);

            // If the entities directory doesn't exist, it's empty
            if (!Directory.Exists(entitiesPath))
            {
                logger.LogInformation("Entities directory does not exist at {Path}. Data folder is considered empty.", entitiesPath);
                return true;
            }

            // Check if there are any subdirectories (entity type folders)
            var entityDirectories = Directory.GetDirectories(entitiesPath);
            
            if (entityDirectories.Length == 0)
            {
                logger.LogInformation("No entity directories found in {Path}. Data folder is empty.", entitiesPath);
                return true;
            }

            // Check if any entity directory contains JSON files (actual data)
            foreach (var entityDir in entityDirectories)
            {
                var jsonFiles = Directory.GetFiles(entityDir, "*.json")
                    .Where(f => !Path.GetFileName(f).Equals(options.IndexFileName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (jsonFiles.Length > 0)
                {
                    logger.LogInformation("Found {Count} data files in {Path}. Data folder is not empty.", 
                        jsonFiles.Length, entityDir);
                    return false;
                }
            }

            logger.LogInformation("No data files found in any entity directory. Data folder is empty.");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if data folder is empty. Assuming not empty to avoid accidental reseeding.");
            return false; // Fail safe: assume not empty to avoid accidental data loss
        }
    }
}
