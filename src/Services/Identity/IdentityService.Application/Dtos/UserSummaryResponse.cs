namespace IdentityService.Application.Dtos;

public record UserSummaryResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime CreatedAt,
    string? PhoneNumber = null
);
