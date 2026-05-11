using IdentityService.Application.Abstractions;
using IdentityService.Application.Dtos;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher hasher,
        IJwtTokenGenerator jwt)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task Register(RegisterRequest request)
    {
        if (await _users.ExistsByEmailAsync(request.Email))
            throw new Exception("User already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _hasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };

        await _users.AddAsync(user);
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email);

        if (user == null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        var accessToken = _jwt.GenerateToken(user);

        var refreshToken = Guid.NewGuid().ToString();

        await _refreshTokens.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        return new AuthResponse(accessToken, refreshToken);
    }
}