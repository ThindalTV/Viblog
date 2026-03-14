using System.Reflection;

namespace Viblog.Admin;

/// <summary>
/// Provides build-time metadata baked into the assembly at publish time.
/// </summary>
internal static class BuildInfo
{
    /// <summary>
    /// The UTC timestamp when the application was built, formatted as "yyyy-MM-dd HH:mm".
    /// </summary>
    public static string PublishDate { get; } =
        typeof(BuildInfo).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildDate")?.Value ?? "unknown";
}
