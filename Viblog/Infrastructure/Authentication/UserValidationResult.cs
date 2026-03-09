namespace Viblog.Infrastructure.Authentication;

/// <summary>
/// Result of user data validation
/// </summary>
public class UserValidationResult
{
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation error messages
    /// </summary>
    public List<string> Errors { get; init; } = [];

    /// <summary>
    /// Create a successful validation result
    /// </summary>
    public static UserValidationResult Valid() => new()
    {
        IsValid = true
    };

    /// <summary>
    /// Create a failed validation result
    /// </summary>
    public static UserValidationResult Invalid(params string[] errors) => new()
    {
        IsValid = false,
        Errors = [.. errors]
    };

    /// <summary>
    /// Create a failed validation result with a list of errors
    /// </summary>
    public static UserValidationResult Invalid(IEnumerable<string> errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}
