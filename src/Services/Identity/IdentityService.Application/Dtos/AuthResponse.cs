namespace IdentityService.Application.Dtos;
public record AuthResponse(
    string AccessToken,
    string RefreshToken
);