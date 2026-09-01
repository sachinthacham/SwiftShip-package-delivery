using BuildingBlocks.Common;
using IdentityService.Application.Dtos;

namespace IdentityService.Application.Abstractions;

public interface IAuthService
{
    Task Register(RegisterRequest request);
    Task CreateUserAsAdmin(AdminCreateUserRequest request);
    Task<AuthResponse> Login(LoginRequest request);
    Task<AuthResponse> Refresh(RefreshTokenRequest request);
    Task Logout(RefreshTokenRequest request);
    Task<MeResponse> GetCurrentUser(Guid userId);
    Task ChangePassword(Guid userId, ChangePasswordRequest request);
    Task<PaginatedList<UserSummaryResponse>> GetUsers(int pageNumber, int pageSize);
}
