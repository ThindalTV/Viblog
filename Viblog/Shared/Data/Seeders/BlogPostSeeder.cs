using Microsoft.Extensions.Logging;
using Viblog.Infrastructure.Shared.Data.Entities;
using Viblog.Infrastructure.Shared.Data.Entities.Content;
using Viblog.Infrastructure.Shared.Data.Repositories;
using Viblog.Shared.Services.Content;

namespace Viblog.Shared.Data.Seeders;

/// <summary>
/// Seeds blog posts using repository pattern (provider-agnostic).
/// Works with any data provider (CosmosDB, Filesystem, SQL, etc.).
/// </summary>
public class BlogPostSeeder
{
    private readonly IBlogPostRepository _repository;
    private readonly ContentProcessingService _processingService;
    private readonly ILogger<BlogPostSeeder> _logger;

    public BlogPostSeeder(
        IBlogPostRepository repository,
        ContentProcessingService processingService,
        ILogger<BlogPostSeeder> logger)
    {
        _repository = repository;
        _processingService = processingService;
        _logger = logger;
    }

    /// <summary>
    /// Seeds blog posts if the repository is empty.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Check if posts already exist
        var existingCount = await _repository.CountAsync(cancellationToken: cancellationToken);
        if (existingCount > 0)
        {
            _logger.LogInformation("Database already contains {Count} blog posts, skipping seed", existingCount);
            return;
        }

        _logger.LogInformation("Seeding blog posts...");

        var now = DateTimeOffset.UtcNow;
        var posts = new List<BlogPost>();

        // Featured Post 1 - Recent (Published)
        posts.Add(CreatePublishedPost(
            title: "Getting Started with Blazor Server in .NET 10",
            slug: "getting-started-blazor-server-net10",
            shortDesc: "Discover the latest features and improvements in Blazor Server for .NET 10, including enhanced performance and new component capabilities.",
            markdown: "# Getting Started with Blazor Server in .NET 10\n\nBlazor Server has evolved significantly with the release of .NET 10...",
            html: "<h1>Getting Started with Blazor Server in .NET 10</h1><p>Blazor Server has evolved significantly with the release of .NET 10...</p>",
            featuredImage: "/img/blog/blazor-server.jpg",
            featuredImageAlt: "Blazor Server architecture diagram",
            metaDescription: "Learn how to get started with Blazor Server in .NET 10",
            tags: ["Blazor", ".NET 10", "Web Development", "Server-Side"],
            categoryIds: ["blazor", "dotnet"],
            publishedAt: now.AddDays(-5),
            isFeatured: true,
            viewCount: 245,
            readingMinutes: 8
        ));

        // Featured Post 2 - Recent (Published)
        posts.Add(CreatePublishedPost(
            title: "Building Scalable Applications with CosmosDB and EF Core",
            slug: "building-scalable-apps-cosmosdb-efcore",
            shortDesc: "Learn best practices for designing and implementing scalable cloud applications using Azure CosmosDB with Entity Framework Core.",
            markdown: "# Building Scalable Applications\n\nCosmosDB offers unparalleled scalability for cloud applications...",
            html: "<h1>Building Scalable Applications</h1><p>CosmosDB offers unparalleled scalability for cloud applications...</p>",
            featuredImage: "/img/blog/cosmosdb.jpg",
            featuredImageAlt: "Azure CosmosDB logo",
            metaDescription: "Best practices for building scalable applications with CosmosDB",
            tags: ["CosmosDB", "Azure", "Entity Framework", "Scalability"],
            categoryIds: ["azure", "databases"],
            publishedAt: now.AddDays(-12),
            isFeatured: true,
            viewCount: 432,
            readingMinutes: 12
        ));

        // Featured Post 3 - Recent (Published)
        posts.Add(CreatePublishedPost(
            title: "Modern CSS Techniques for Responsive Web Design",
            slug: "modern-css-responsive-design",
            shortDesc: "Explore cutting-edge CSS features including Grid, Flexbox, and Container Queries for creating truly responsive web applications.",
            markdown: "# Modern CSS Techniques\n\nCSS has come a long way with modern layout systems...",
            html: "<h1>Modern CSS Techniques</h1><p>CSS has come a long way with modern layout systems...</p>",
            featuredImage: "/img/blog/css-modern.jpg",
            featuredImageAlt: "Modern CSS layout examples",
            metaDescription: "Learn modern CSS techniques for responsive web design",
            tags: ["CSS", "Web Design", "Responsive", "Front-End"],
            categoryIds: ["frontend", "css"],
            publishedAt: now.AddDays(-18),
            isFeatured: true,
            viewCount: 521,
            readingMinutes: 10
        ));

        // Additional published posts
        posts.AddRange([
            CreatePublishedPost("Understanding Dependency Injection in .NET", "dependency-injection-dotnet",
                "A comprehensive guide to dependency injection patterns and best practices in .NET applications.",
                ["Dependency Injection", ".NET", "Design Patterns"], ["dotnet", "architecture"],
                now.AddDays(-25), 198, 9),

            CreatePublishedPost("Building RESTful APIs with ASP.NET Core", "restful-apis-aspnet-core",
                "Learn how to design and implement robust RESTful APIs using ASP.NET Core.",
                ["ASP.NET Core", "REST API", "Web Services"], ["aspnet", "api"],
                now.AddDays(-32), 287, 11),

            CreatePublishedPost("Docker Containerization for .NET Applications", "docker-dotnet-containerization",
                "Step-by-step guide to containerizing your .NET applications with Docker.",
                ["Docker", ".NET", "DevOps", "Containers"], ["devops", "docker"],
                now.AddDays(-40), 345, 10),

            CreatePublishedPost("Introduction to Microservices Architecture", "microservices-architecture-intro",
                "Understanding microservices patterns and when to use them in your applications.",
                ["Microservices", "Architecture", "Distributed Systems"], ["architecture", "microservices"],
                now.AddDays(-47), 412, 14),

            CreatePublishedPost("SignalR Real-Time Communication in Blazor", "signalr-blazor-realtime",
                "Build real-time features in your Blazor applications using SignalR.",
                ["SignalR", "Blazor", "Real-Time", "WebSockets"], ["blazor", "signalr"],
                now.AddDays(-54), 289, 8),

            CreatePublishedPost("Cloud-Native Development with Azure", "cloud-native-azure-development",
                "Best practices for building cloud-native applications on Microsoft Azure.",
                ["Azure", "AWS", "Serverless", "Cloud"], ["azure", "cloud"],
                now.AddDays(-60), 367, 10),

            CreatePublishedPost("SOLID Principles Explained with C# Examples", "solid-principles-csharp-examples",
                "Understanding SOLID principles through practical C# code examples and real-world scenarios.",
                ["C#", "SOLID", "Design Patterns", "Best Practices"], ["csharp", "architecture"],
                now.AddDays(-67), 489, 13),

            CreatePublishedPost("Introduction to GraphQL with .NET", "graphql-dotnet-introduction",
                "Learn how to build GraphQL APIs using .NET and Hot Chocolate framework.",
                ["GraphQL", ".NET", "API", "Hot Chocolate"], ["dotnet", "api"],
                now.AddDays(-74), 203, 9),

            CreatePublishedPost("Performance Optimization Tips for Blazor Apps", "blazor-performance-optimization",
                "Practical tips and techniques to improve the performance of your Blazor applications.",
                ["Blazor", "Performance", "Optimization"], ["blazor", "performance"],
                now.AddDays(-82), 341, 8),

            CreatePublishedPost("Unit Testing Best Practices with xUnit", "unit-testing-xunit-best-practices",
                "Write better unit tests with xUnit, including mocking, test organization, and CI/CD integration.",
                ["Testing", "xUnit", "TDD", "Best Practices"], ["testing", "quality"],
                now.AddDays(-89), 278, 10),

            CreatePublishedPost("Securing ASP.NET Core Applications", "securing-aspnet-core-applications",
                "Essential security practices for protecting your ASP.NET Core web applications.",
                ["Security", "ASP.NET Core", "Authentication", "Authorization"], ["security", "aspnet"],
                now.AddDays(-96), 456, 12),

            CreatePublishedPost("Exploring Minimal APIs in .NET", "minimal-apis-dotnet-exploration",
                "Discover the simplicity and power of Minimal APIs introduced in recent .NET versions.",
                [".NET", "Minimal APIs", "Web Development"], ["dotnet", "api"],
                now.AddDays(-103), 312, 7)
        ]);

        // Update search indexes for all posts
        foreach (var post in posts)
        {
            var tagText = string.Join(" ", post.Tags);
            var categoryText = string.Join(" ", post.CategoryNames);
            _processingService.UpdateSearchIndex(post.Draft, $"{tagText} {categoryText}");
            if (post.Live != null)
            {
                _processingService.UpdateSearchIndex(post.Live, $"{tagText} {categoryText}");
            }
        }

        // Add all posts using repository
        await _repository.AddRangeAsync(posts, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully seeded {Count} blog posts", posts.Count);
    }

    /// <summary>
    /// Creates a published blog post with both Draft and Live content.
    /// </summary>
    private BlogPost CreatePublishedPost(
        string title,
        string slug,
        string shortDesc,
        string[] tags,
        string[] categoryIds,
        DateTimeOffset publishedAt,
        int viewCount,
        int readingMinutes,
        string? markdown = null,
        string? html = null,
        string? featuredImage = null,
        string? featuredImageAlt = null,
        string? metaDescription = null,
        bool isFeatured = false)
    {
        // Generate default content if not provided
        markdown ??= $"# {title}\n\n{shortDesc}\n\nThis is sample content for demonstration purposes.";
        html ??= $"<h1>{title}</h1><p>{shortDesc}</p><p>This is sample content for demonstration purposes.</p>";

        var content = new BlogPostContent
        {
            Title = title,
            Short = shortDesc,
            Markdown = markdown,
            Content = html,
            FeaturedImageUrl = featuredImage,
            FeaturedImageAlt = featuredImageAlt,
            MetaDescription = metaDescription ?? shortDesc
        };
        content.ComputeHash();

        // Derive category names from IDs (simplified - in real app might look up from Category repository)
        var categoryNames = categoryIds.Select(id => 
            char.ToUpper(id[0]) + id.Substring(1)).ToArray();

        var post = new BlogPost
        {
            IsPublished = true,
            Id = Guid.NewGuid().ToString(),
            Slug = slug,
            AuthorId = "system",
            AuthorName = "System Administrator",
            PublishedAt = publishedAt,
            IsFeatured = isFeatured,
            ViewCount = viewCount,
            ReadingTimeMinutes = readingMinutes,
            Tags = [.. tags],
            CategoryIds = [.. categoryIds],
            CategoryNames = [.. categoryNames],
            CreatedAt = publishedAt,
            UpdatedAt = publishedAt,
            // Both Draft and Live have same content for published posts
            Draft = content,
            Live = new BlogPostContent
            {
                Title = content.Title,
                Short = content.Short,
                Markdown = content.Markdown,
                Content = content.Content,
                FeaturedImageUrl = content.FeaturedImageUrl,
                FeaturedImageAlt = content.FeaturedImageAlt,
                MetaDescription = content.MetaDescription,
                ContentHash = content.ContentHash
            },
            Schedule = new ContentSchedule
            {
                Status = ContentStatus.Draft,
                PublishedAt = publishedAt
            }
        };

        return post;
    }
}
