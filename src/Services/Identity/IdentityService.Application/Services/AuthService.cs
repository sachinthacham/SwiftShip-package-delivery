using BuildingBlocks.Common;
using BuildingBlocks.Exceptions;
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

    public Task Register(RegisterRequest request)
        => CreateUserAsync(request.Email, request.Password, request.FirstName, request.LastName, request.Role, request.PhoneNumber);

    public Task CreateUserAsAdmin(AdminCreateUserRequest request)
        => CreateUserAsync(request.Email, request.Password, request.FirstName, request.LastName, request.Role, request.PhoneNumber);

    private async Task CreateUserAsync(string email, string password, string firstName, string lastName, string role, string? phoneNumber)
    {
        if (await _users.ExistsByEmailAsync(email))
            throw new ConflictException("A user with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(password),
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        await _users.AddAsync(user);
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email);

        if (user == null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> Refresh(RefreshTokenRequest request)
    {
        var existing = await _refreshTokens.GetByTokenAsync(request.RefreshToken);

        if (existing == null || existing.IsRevoked || existing.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");

        var user = await _users.GetByIdAsync(existing.UserId)
            ?? throw new UnauthorizedAccessException("Refresh token is invalid or expired.");

        await _refreshTokens.RevokeAsync(existing);

        return await IssueTokensAsync(user);
    }

    public async Task Logout(RefreshTokenRequest request)
    {
        var existing = await _refreshTokens.GetByTokenAsync(request.RefreshToken);
        if (existing != null && !existing.IsRevoked)
        {
            await _refreshTokens.RevokeAsync(existing);
        }
    }

    public async Task<MeResponse> GetCurrentUser(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        return ToMeResponse(user);
    }

    public async Task ChangePassword(Guid userId, ChangePasswordRequest request)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        await _users.UpdateAsync(user);
    }

    public async Task<PaginatedList<UserSummaryResponse>> GetUsers(int pageNumber, int pageSize)
    {
        var page = await _users.GetPagedAsync(pageNumber, pageSize);
        var items = page.Items.Select(ToSummaryResponse).ToList();

        return new PaginatedList<UserSummaryResponse>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }

    private static MeResponse ToMeResponse(User user)
        => new(user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.CreatedAt, user.PhoneNumber);

    private static UserSummaryResponse ToSummaryResponse(User user)
        => new(user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.CreatedAt, user.PhoneNumber);

    private async Task<AuthResponse> IssueTokensAsync(User user)
    {
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