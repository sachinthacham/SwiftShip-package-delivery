using IdentityService.Application.Dtos;

namespace IdentityService.Application.Abstractions;

public interface ISavedAddressService
{
    Task<SavedAddressResponse> Create(Guid userId, CreateSavedAddressRequest request);
    Task<List<SavedAddressResponse>> GetForUser(Guid userId);
    Task<SavedAddressResponse> GetById(Guid userId, Guid id);
    Task<SavedAddressResponse> Update(Guid userId, Guid id, UpdateSavedAddressRequest request);
    Task Delete(Guid userId, Guid id);
    Task<SavedAddressResponse> SetDefault(Guid userId, Guid id);
}
