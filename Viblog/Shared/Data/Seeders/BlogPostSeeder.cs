using Microsoft.EntityFrameworkCore;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Shared.Data.Seeders;

/// <summary>
/// Seeds the database with initial blog post data
/// </summary>
public static class BlogPostSeeder
{
    /// <summary>
    /// Seed blog posts if the database is empty
    /// </summary>
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Check if we already have blog posts by trying to get the first one
        // Use FromSqlRaw for CosmosDB compatibility
        var hasData = await context.Set<BlogPost>()
            .AsNoTracking()
            .Take(1)
            .ToListAsync();

        if (hasData.Any())
        {
            return; // Database already has data
        }

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
            AllowComments = true,
            ViewCount = 245,
            Tags = new List<string> { "Blazor", ".NET 10", "Web Development", "Server-Side" },
            CategoryIds = new List<string> { "blazor", "dotnet" },
            CategoryNames = new List<string> { "Blazor", ".NET" },
            MetaDescription = "Learn how to get started with Blazor Server in .NET 10",
            ReadingTimeMinutes = 8,
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-5)
        };
        post1.UpdatePartitionKey(); // Set partition key based on publication year

        var comment1 = new Comment
        {
            Id = Guid.NewGuid().ToString(),
            AuthorName = "John Developer",
            AuthorEmail = "john@example.com",
            AuthorWebsite = "https://johndeveloper.com",
            Content = "Great article! The new features in Blazor Server .NET 10 are really impressive. I especially like the performance improvements.",
            CreatedAt = now.AddDays(-4),
            IsApproved = true
        };

        var comment2 = new Comment
        {
            Id = Guid.NewGuid().ToString(),
            AuthorName = "Sarah Tech",
            AuthorEmail = "sarah@example.com",
            Content = "Thanks for sharing this! One question - how does the new component lifecycle compare to the previous version?",
            CreatedAt = now.AddDays(-3),
            IsApproved = true
        };

        var comment3 = new Comment
        {
            Id = Guid.NewGuid().ToString(),
            ParentCommentId = comment2.Id,
            AuthorName = "System Administrator",
            AuthorEmail = "admin@example.com",
            Content = "Good question! The lifecycle has been optimized for better predictability. I'll write a detailed post about this soon.",
            CreatedAt = now.AddDays(-2),
            IsApproved = true
        };

        post1.Comments = new List<Comment> { comment1, comment2, comment3 };
        post1.CommentCount = 3;
        post1.LastCommentAt = now.AddDays(-2);

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
            AllowComments = true,
            ViewCount = 432,
            Tags = new List<string> { "CosmosDB", "Azure", "Entity Framework", "Scalability" },
            CategoryIds = new List<string> { "azure", "databases" },
            CategoryNames = new List<string> { "Azure", "Databases" },
            MetaDescription = "Best practices for building scalable applications with CosmosDB",
            ReadingTimeMinutes = 12,
            CreatedAt = now.AddDays(-12),
            UpdatedAt = now.AddDays(-12),
            Comments = new List<Comment>
            {
                new Comment
                {
                    Id = Guid.NewGuid().ToString(),
                    AuthorName = "Michael Cloud",
                    AuthorEmail = "michael@example.com",
                    Content = "This is exactly what I needed! We're migrating to CosmosDB next quarter and this guide is invaluable.",
                    CreatedAt = now.AddDays(-10),
                    IsApproved = true
                },
                new Comment
                {
                    Id = Guid.NewGuid().ToString(),
                    AuthorName = "Emily Data",
                    AuthorEmail = "emily@example.com",
                    AuthorWebsite = "https://emilydata.blog",
                    Content = "Could you elaborate on partition key strategies? That's where I'm struggling the most.",
                    CreatedAt = now.AddDays(-8),
                    IsApproved = true
                }
            },
            CommentCount = 2,
            LastCommentAt = now.AddDays(-8)
        };
        post2.UpdatePartitionKey();
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
            AllowComments = true,
            ViewCount = 389,
            Tags = new List<string> { "CSS", "Responsive Design", "Web Development", "Frontend" },
            CategoryIds = new List<string> { "frontend", "css" },
            CategoryNames = new List<string> { "Frontend", "CSS" },
            MetaDescription = "Learn modern CSS techniques for responsive web design",
            ReadingTimeMinutes = 10,
            CreatedAt = now.AddDays(-18),
            UpdatedAt = now.AddDays(-18)
        };
        post3.UpdatePartitionKey();
        posts.Add(post3);

        // Regular Posts
        posts.AddRange(new[]
        {
            CreateBlogPost("Understanding Dependency Injection in ASP.NET Core", "dependency-injection-aspnet-core",
                "A deep dive into the dependency injection container in ASP.NET Core and how to use it effectively.",
                new[] { "ASP.NET Core", "DI", "Architecture" }, new[] { "aspnet", "architecture" },
                now.AddDays(-25), 156, 7),

            CreateBlogPost("Introduction to Docker for .NET Developers", "docker-dotnet-developers",
                "Learn how to containerize your .NET applications using Docker for improved deployment and scalability.",
                new[] { "Docker", ".NET", "DevOps", "Containers" }, new[] { "devops", "docker" },
                now.AddDays(-32), 298, 9),

            CreateBlogPost("Async/Await Best Practices in C#", "async-await-best-practices-csharp",
                "Master asynchronous programming in C# with practical examples and common pitfalls to avoid.",
                new[] { "C#", "Async", "Best Practices", "Performance" }, new[] { "csharp", "performance" },
                now.AddDays(-38), 521, 11),

            CreateBlogPost("RESTful API Design Principles", "restful-api-design-principles",
                "Essential principles and patterns for designing clean, maintainable RESTful APIs.",
                new[] { "API", "REST", "Design", "Architecture" }, new[] { "api", "architecture" },
                now.AddDays(-45), 412, 8),

            CreateBlogPost("Getting Started with Telerik UI for Blazor", "telerik-ui-blazor-getting-started",
                "An introduction to Telerik UI components for Blazor and how to integrate them into your projects.",
                new[] { "Blazor", "Telerik", "UI Components" }, new[] { "blazor", "ui" },
                now.AddDays(-52), 234, 6),

            CreateBlogPost("Azure Functions vs AWS Lambda: A Comparison", "azure-functions-vs-aws-lambda",
                "Compare serverless computing platforms and learn which one is right for your project.",
                new[] { "Azure", "AWS", "Serverless", "Cloud" }, new[] { "azure", "cloud" },
                now.AddDays(-60), 367, 10),

            CreateBlogPost("SOLID Principles Explained with C# Examples", "solid-principles-csharp-examples",
                "Understanding SOLID principles through practical C# code examples and real-world scenarios.",
                new[] { "C#", "SOLID", "Design Patterns", "Best Practices" }, new[] { "csharp", "architecture" },
                now.AddDays(-67), 489, 13),

            CreateBlogPost("Introduction to GraphQL with .NET", "graphql-dotnet-introduction",
                "Learn how to build GraphQL APIs using .NET and Hot Chocolate framework.",
                new[] { "GraphQL", ".NET", "API", "Hot Chocolate" }, new[] { "dotnet", "api" },
                now.AddDays(-74), 203, 9),

            CreateBlogPost("Performance Optimization Tips for Blazor Apps", "blazor-performance-optimization",
                "Practical tips and techniques to improve the performance of your Blazor applications.",
                new[] { "Blazor", "Performance", "Optimization" }, new[] { "blazor", "performance" },
                now.AddDays(-82), 341, 8),

            CreateBlogPost("Unit Testing Best Practices with xUnit", "unit-testing-xunit-best-practices",
                "Write better unit tests with xUnit, including mocking, test organization, and CI/CD integration.",
                new[] { "Testing", "xUnit", "TDD", "Best Practices" }, new[] { "testing", "quality" },
                now.AddDays(-89), 278, 10),

            CreateBlogPost("Securing ASP.NET Core Applications", "securing-aspnet-core-applications",
                "Essential security practices for protecting your ASP.NET Core web applications.",
                new[] { "Security", "ASP.NET Core", "Authentication", "Authorization" }, new[] { "security", "aspnet" },
                now.AddDays(-96), 456, 12),

            CreateBlogPost("Exploring Minimal APIs in .NET", "minimal-apis-dotnet-exploration",
                "Discover the simplicity and power of Minimal APIs introduced in recent .NET versions.",
                new[] { ".NET", "Minimal APIs", "Web Development" }, new[] { "dotnet", "api" },
                now.AddDays(-103), 312, 7)
        });

        // Update search indexes for all posts
        foreach (var post in posts)
        {
            post.UpdateSearchIndex();
        }

        // Add all posts to context
        await context.Set<BlogPost>().AddRangeAsync(posts);
        await context.SaveChangesAsync();
    }

    private static BlogPost CreateBlogPost(
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
            AllowComments = true,
            ViewCount = viewCount,
            Tags = tags.ToList(),
            CategoryIds = categories.ToList(),
            CategoryNames = categoryNames,
            MetaDescription = shortDescription,
            ReadingTimeMinutes = readingTime,
            CreatedAt = publishedAt,
            UpdatedAt = publishedAt
        };
        
        post.UpdatePartitionKey(); // Set partition key based on publication year
        return post;
    }
}
