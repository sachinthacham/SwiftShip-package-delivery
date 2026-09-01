namespace IdentityService.Application.Dtos;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
