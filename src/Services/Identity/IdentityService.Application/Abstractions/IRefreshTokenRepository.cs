using IdentityService.Domain.Entities;

namespace IdentityService.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken);
}
