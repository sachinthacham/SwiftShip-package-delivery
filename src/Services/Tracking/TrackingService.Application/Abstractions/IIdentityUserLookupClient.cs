namespace TrackingService.Application.Abstractions;

public record UserContact(Guid Id, string Email, string FirstName, string LastName, string? PhoneNumber = null);

public interface IIdentityUserLookupClient
{
    /// <summary>Returns the user's contact details, or null if the user does not exist or the lookup fails.</summary>
    Task<UserContact?> GetUserContactAsync(Guid userId, CancellationToken cancellationToken = default);
}
