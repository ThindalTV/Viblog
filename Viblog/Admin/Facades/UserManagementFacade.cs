using Viblog.Infrastructure.Admin.Facades;
using Viblog.Infrastructure.Shared.Authentication;
using Viblog.Infrastructure.Shared.Data.Common;
using Viblog.Infrastructure.Shared.Data.Entities;

namespace Viblog.Admin.Facades;

/// <summary>
/// Facade implementation for admin user management operations
/// </summary>
public class UserManagementFacade : IUserManagementFacade
{
    private readonly IUserManagementService _userManagementService;

    public UserManagementFacade(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
    }

    /// <inheritdoc/>
    public virtual async Task<PagedResult<AdminUser>> GetUsersAsync(
        PagingParameters pagingParameters,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagingParameters);

        return await _userManagementService.GetUsersAsync(pagingParameters, includeInactive, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<AdminUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _userManagementService.GetUserByIdAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<(AdminUser? User, UserValidationResult ValidationResult)> CreateUserAsync(
        string name,
        string email,
        string password,
        IEnumerable<string> claims,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentNullException.ThrowIfNull(claims);

        return await _userManagementService.CreateUserAsync(name, email, password, claims, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<(AdminUser? User, UserValidationResult ValidationResult)> UpdateUserAsync(
        string userId,
        string name,
        string email,
        IEnumerable<string> claims,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(claims);

        return await _userManagementService.UpdateUserAsync(userId, name, email, claims, isActive, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _userManagementService.DeleteUserAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAvailableClaims()
    {
        return UserClaims.AllClaims;
    }

    /// <inheritdoc/>
    public virtual async Task<UserValidationResult> ResetPasswordAsync(
        string userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        return await _userManagementService.ResetPasswordAsync(userId, newPassword, cancellationToken);
    }
}
