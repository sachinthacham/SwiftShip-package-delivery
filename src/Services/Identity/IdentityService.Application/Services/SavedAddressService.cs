using IdentityService.Application.Abstractions;
using IdentityService.Application.Dtos;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Services;

public class SavedAddressService : ISavedAddressService
{
    private readonly ISavedAddressRepository _addresses;

    public SavedAddressService(ISavedAddressRepository addresses)
    {
        _addresses = addresses;
    }

    public async Task<SavedAddressResponse> Create(Guid userId, CreateSavedAddressRequest request)
    {
        var address = new SavedAddress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Label = request.Label,
            Street = request.Street,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        if (address.IsDefault)
        {
            await UnsetOtherDefaultsAsync(userId, address.Id);
        }

        await _addresses.AddAsync(address);

        return ToResponse(address);
    }

    public async Task<List<SavedAddressResponse>> GetForUser(Guid userId)
    {
        var addresses = await _addresses.GetByUserIdAsync(userId);
        return addresses.Select(ToResponse).ToList();
    }

    public async Task<SavedAddressResponse> GetById(Guid userId, Guid id)
    {
        var address = await GetOwnedAsync(userId, id);
        return ToResponse(address);
    }

    public async Task<SavedAddressResponse> Update(Guid userId, Guid id, UpdateSavedAddressRequest request)
    {
        var address = await GetOwnedAsync(userId, id);

        address.Label = request.Label;
        address.Street = request.Street;
        address.City = request.City;
        address.State = request.State;
        address.PostalCode = request.PostalCode;
        address.Country = request.Country;
        address.Latitude = request.Latitude;
        address.Longitude = request.Longitude;

        await _addresses.UpdateAsync(address);

        return ToResponse(address);
    }

    public async Task Delete(Guid userId, Guid id)
    {
        var address = await GetOwnedAsync(userId, id);
        await _addresses.DeleteAsync(address);
    }

    public async Task<SavedAddressResponse> SetDefault(Guid userId, Guid id)
    {
        var address = await GetOwnedAsync(userId, id);

        await UnsetOtherDefaultsAsync(userId, id);

        address.IsDefault = true;
        await _addresses.UpdateAsync(address);

        return ToResponse(address);
    }

    private async Task<SavedAddress> GetOwnedAsync(Guid userId, Guid id)
    {
        var address = await _addresses.GetByIdAsync(id);

        if (address == null || address.UserId != userId)
            throw new KeyNotFoundException("Saved address not found.");

        return address;
    }

    private async Task UnsetOtherDefaultsAsync(Guid userId, Guid exceptId)
    {
        var existing = await _addresses.GetByUserIdAsync(userId);
        var toUnset = existing.Where(a => a.Id != exceptId && a.IsDefault).ToList();

        if (toUnset.Count == 0)
            return;

        foreach (var address in toUnset)
        {
            address.IsDefault = false;
        }

        await _addresses.UpdateRangeAsync(toUnset);
    }

    private static SavedAddressResponse ToResponse(SavedAddress address)
        => new(
            address.Id,
            address.Label,
            address.Street,
            address.City,
            address.State,
            address.PostalCode,
            address.Country,
            address.Latitude,
            address.Longitude,
            address.IsDefault,
            address.CreatedAt);
}
