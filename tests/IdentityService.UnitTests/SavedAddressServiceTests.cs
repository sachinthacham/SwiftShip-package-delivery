using IdentityService.Application.Abstractions;
using IdentityService.Application.Dtos;
using IdentityService.Application.Services;
using IdentityService.Domain.Entities;
using Moq;

namespace IdentityService.UnitTests;

public class SavedAddressServiceTests
{
    private readonly Mock<ISavedAddressRepository> _addresses = new();

    private SavedAddressService CreateSut() => new(_addresses.Object);

    private static CreateSavedAddressRequest MakeCreateRequest(bool isDefault = false) =>
        new("Home", "1 Main St", "Springfield", "IL", "62701", "USA", 39.78, -89.65, isDefault);

    [Fact]
    public async Task Create_AddsAddress_ForGivenUser()
    {
        var userId = Guid.NewGuid();
        var request = MakeCreateRequest();

        var sut = CreateSut();

        var result = await sut.Create(userId, request);

        Assert.Equal(request.Label, result.Label);
        _addresses.Verify(a => a.AddAsync(It.Is<SavedAddress>(x => x.UserId == userId && x.Label == "Home")), Times.Once);
    }

    [Fact]
    public async Task Create_UnsetsOtherDefaults_WhenNewAddressIsDefault()
    {
        var userId = Guid.NewGuid();
        var existingDefault = new SavedAddress { Id = Guid.NewGuid(), UserId = userId, IsDefault = true, Label = "Work", Street = "s", City = "c", State = "s", PostalCode = "p", Country = "c" };
        _addresses.Setup(a => a.GetByUserIdAsync(userId)).ReturnsAsync([existingDefault]);

        var sut = CreateSut();

        await sut.Create(userId, MakeCreateRequest(isDefault: true));

        _addresses.Verify(a => a.UpdateRangeAsync(It.Is<IEnumerable<SavedAddress>>(list => list.Contains(existingDefault) && !existingDefault.IsDefault)), Times.Once);
    }

    [Fact]
    public async Task GetForUser_ReturnsOnlyThatUsersAddresses()
    {
        var userId = Guid.NewGuid();
        var addresses = new List<SavedAddress>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Label = "Home", Street = "s", City = "c", State = "s", PostalCode = "p", Country = "c" }
        };
        _addresses.Setup(a => a.GetByUserIdAsync(userId)).ReturnsAsync(addresses);

        var sut = CreateSut();

        var result = await sut.GetForUser(userId);

        Assert.Single(result);
        Assert.Equal("Home", result[0].Label);
    }

    [Fact]
    public async Task Update_Throws_WhenAddressBelongsToDifferentUser()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var address = new SavedAddress { Id = Guid.NewGuid(), UserId = ownerId, Label = "Home", Street = "s", City = "c", State = "s", PostalCode = "p", Country = "c" };
        _addresses.Setup(a => a.GetByIdAsync(address.Id)).ReturnsAsync(address);

        var sut = CreateSut();

        var request = new UpdateSavedAddressRequest("Home", "2 Other St", "c", "s", "p", "c", null, null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.Update(otherUserId, address.Id, request));
    }

    [Fact]
    public async Task Update_Throws_WhenAddressDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();
        _addresses.Setup(a => a.GetByIdAsync(id)).ReturnsAsync((SavedAddress?)null);

        var sut = CreateSut();

        var request = new UpdateSavedAddressRequest("Home", "s", "c", "s", "p", "c", null, null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.Update(userId, id, request));
    }

    [Fact]
    public async Task Delete_RemovesAddress_WhenOwnedByUser()
    {
        var userId = Guid.NewGuid();
        var address = new SavedAddress { Id = Guid.NewGuid(), UserId = userId, Label = "Home", Street = "s", City = "c", State = "s", PostalCode = "p", Country = "c" };
        _addresses.Setup(a => a.GetByIdAsync(address.Id)).ReturnsAsync(address);

        var sut = CreateSut();

        await sut.Delete(userId, address.Id);

        _addresses.Verify(a => a.DeleteAsync(address), Times.Once);
    }

    [Fact]
    public async Task SetDefault_MarksAddressDefault_AndUnsetsOthers()
    {
        var userId = Guid.NewGuid();
        var target = new SavedAddress { Id = Guid.NewGuid(), UserId = userId, IsDefault = false, Label = "Home", Street = "s", City = "c", State = "s", PostalCode = "p", Country = "c" };
        var other = new SavedAddress { Id = Guid.NewGuid(), UserId = userId, IsDefault = true, Label = "Work", Street = "s", City = "c", State = "s", PostalCode = "p", Country = "c" };

        _addresses.Setup(a => a.GetByIdAsync(target.Id)).ReturnsAsync(target);
        _addresses.Setup(a => a.GetByUserIdAsync(userId)).ReturnsAsync([target, other]);

        var sut = CreateSut();

        var result = await sut.SetDefault(userId, target.Id);

        Assert.True(result.IsDefault);
        Assert.False(other.IsDefault);
        _addresses.Verify(a => a.UpdateAsync(It.Is<SavedAddress>(x => x.Id == target.Id && x.IsDefault)), Times.Once);
    }
}
