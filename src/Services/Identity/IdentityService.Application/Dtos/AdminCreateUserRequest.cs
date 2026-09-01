namespace IdentityService.Application.Dtos;

/// <summary>Used by an Admin to provision a user with any role, including Dispatcher/Admin.</summary>
public record AdminCreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    string? PhoneNumber = null
);
