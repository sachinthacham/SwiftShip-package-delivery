using IdentityService.Application.Dtos;

namespace IdentityService.Application.Abstractions;

public interface IAuthService
{
    Task Register(RegisterRequest request);
    Task<AuthResponse> Login(LoginRequest request);
}
