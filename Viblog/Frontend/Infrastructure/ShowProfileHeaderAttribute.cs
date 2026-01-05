namespace Vilog.Frontend.Infrastructure;

/// <summary>
/// Attribute to indicate that a page should display the profile header
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ShowProfileHeaderAttribute : Attribute
{
}
