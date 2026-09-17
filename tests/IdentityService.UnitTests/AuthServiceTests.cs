using BuildingBlocks.Common;
using BuildingBlocks.Exceptions;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Dtos;
using IdentityService.Application.Services;
using IdentityService.Domain.Entities;
using Moq;

namespace IdentityService.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();

    private AuthService CreateSut() => new(_users.Object, _refreshTokens.Object, _hasher.Object, _jwt.Object);

    [Fact]
    public async Task Register_AddsUser_WhenEmailNotTaken()
    {
        var request = new RegisterRequest("new@user.com", "Password1!", "New", "User");
        _users.Setup(u => u.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash(request.Password)).Returns("hashed");

        var sut = CreateSut();

        await sut.Register(request);

        _users.Verify(u => u.AddAsync(It.Is<User>(x =>
            x.Email == request.Email &&
            x.PasswordHash == "hashed" &&
            x.Role == "Customer")), Times.Once);
    }

    [Fact]
    public async Task Register_Throws_WhenEmailAlreadyExists()
    {
        var request = new RegisterRequest("dup@user.com", "Password1!", "New", "User");
        _users.Setup(u => u.ExistsByEmailAsync(request.Email)).ReturnsAsync(true);

        var sut = CreateSut();

        await Assert.ThrowsAsync<ConflictException>(() => sut.Register(request));
        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsTokens_WhenCredentialsValid()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "a@b.com", PasswordHash = "hashed", FirstName = "A", LastName = "B", Role = "Customer" };
        var request = new LoginRequest(user.Email, "Password1!");

        _users.Setup(u => u.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(request.Password, user.PasswordHash)).Returns(true);
        _jwt.Setup(j => j.GenerateToken(user)).Returns("access-token");

        var sut = CreateSut();

        var result = await sut.Login(request);

        Assert.Equal("access-token", result.AccessToken);
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        _refreshTokens.Verify(r => r.AddAsync(It.Is<RefreshToken>(rt => rt.UserId == user.Id)), Times.Once);
    }

    [Fact]
    public async Task Login_Throws_WhenPasswordIncorrect()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "a@b.com", PasswordHash = "hashed", FirstName = "A", LastName = "B", Role = "Customer" };
        var request = new LoginRequest(user.Email, "WrongPassword!");

        _users.Setup(u => u.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(request.Password, user.PasswordHash)).Returns(false);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.Login(request));
    }

    [Fact]
    public async Task Login_Throws_WhenUserDoesNotExist()
    {
        var request = new LoginRequest("missing@user.com", "Password1!");
        _users.Setup(u => u.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.Login(request));
    }

    [Fact]
    public async Task Refresh_IssuesNewTokens_AndRevokesOld_WhenTokenValid()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "a@b.com", PasswordHash = "hashed", FirstName = "A", LastName = "B", Role = "Customer" };
        var existing = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, Token = "old-token", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false };
        var request = new RefreshTokenRequest("old-token");

        _refreshTokens.Setup(r => r.GetByTokenAsync("old-token")).ReturnsAsync(existing);
        _users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _jwt.Setup(j => j.GenerateToken(user)).Returns("new-access-token");

        var sut = CreateSut();

        var result = await sut.Refresh(request);

        Assert.Equal("new-access-token", result.AccessToken);
        _refreshTokens.Verify(r => r.RevokeAsync(existing), Times.Once);
        _refreshTokens.Verify(r => r.AddAsync(It.Is<RefreshToken>(rt => rt.UserId == user.Id && rt.Token != "old-token")), Times.Once);
    }

    [Fact]
    public async Task Refresh_Throws_WhenTokenRevoked()
    {
        var existing = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Token = "revoked-token", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = true };
        _refreshTokens.Setup(r => r.GetByTokenAsync("revoked-token")).ReturnsAsync(existing);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.Refresh(new RefreshTokenRequest("revoked-token")));
    }

    [Fact]
    public async Task Refresh_Throws_WhenTokenExpired()
    {
        var existing = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Token = "expired-token", ExpiresAt = DateTime.UtcNow.AddDays(-1), IsRevoked = false };
        _refreshTokens.Setup(r => r.GetByTokenAsync("expired-token")).ReturnsAsync(existing);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.Refresh(new RefreshTokenRequest("expired-token")));
    }

    [Fact]
    public async Task ChangePassword_UpdatesHash_WhenCurrentPasswordCorrect()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "a@b.com", PasswordHash = "old-hash", FirstName = "A", LastName = "B", Role = "Customer" };
        var request = new ChangePasswordRequest("OldPassword1!", "NewPassword1!");

        _users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(request.CurrentPassword, user.PasswordHash)).Returns(true);
        _hasher.Setup(h => h.Hash(request.NewPassword)).Returns("new-hash");

        var sut = CreateSut();

        await sut.ChangePassword(user.Id, request);

        _users.Verify(u => u.UpdateAsync(It.Is<User>(x => x.PasswordHash == "new-hash")), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_Throws_WhenCurrentPasswordIncorrect()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "a@b.com", PasswordHash = "old-hash", FirstName = "A", LastName = "B", Role = "Customer" };
        var request = new ChangePasswordRequest("WrongPassword!", "NewPassword1!");

        _users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(request.CurrentPassword, user.PasswordHash)).Returns(false);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.ChangePassword(user.Id, request));
        _users.Verify(u => u.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentUser_Throws_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _users.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.GetCurrentUser(userId));
    }
}
