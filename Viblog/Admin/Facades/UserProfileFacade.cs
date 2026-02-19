using Viblog.Infrastructure.Admin.Facades;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Admin.Facades;

/// <summary>
/// Facade implementation for user profile self-service operations
/// </summary>
public class UserProfileFacade : IUserProfileFacade
{
    private readonly IUserManagementService _userManagementService;
    private readonly IAuthenticationProvider _authenticationProvider;

    public UserProfileFacade(
        IUserManagementService userManagementService,
        IAuthenticationProvider authenticationProvider)
    {
        _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
        _authenticationProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
    }

    /// <inheritdoc/>
    public virtual async Task<ApplicationUser?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _userManagementService.GetUserByIdAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<(ApplicationUser? User, UserValidationResult ValidationResult)> UpdateProfileAsync(
        string userId,
        string name,
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        // Get current user to preserve claims and active status
        var currentUser = await _userManagementService.GetUserByIdAsync(userId, cancellationToken);
        if (currentUser is null)
        {
            return (null, UserValidationResult.Invalid("User not found."));
        }

        // Update using existing claims and active status
        return await _userManagementService.UpdateUserAsync(
            userId,
            name,
            email,
            currentUser.CustomClaims,
            currentUser.IsActive,
            cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<PasswordChangeResult> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        return await _authenticationProvider.ChangePasswordAsync(userId, currentPassword, newPassword, cancellationToken);
    }
}
