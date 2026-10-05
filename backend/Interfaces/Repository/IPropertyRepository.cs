using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IPropertyRepository
    {
        Task<PropertyModel?> GetByIdAsync(int id);
        Task<PropertyModel?> AddAsync(int organizationId, PropertyModel property);
        Task<PropertyModel?> UpdateAsync(PropertyModel property);
        Task<bool> IsInviteCodeValidAsync(string inviteCode, CancellationToken cancellationToken = default);
        Task<string?> RotateInviteIdAsync(int id, PropertyInviteType inviteType);
        Task<PropertyDeleteResult> DeleteAsync(int id);
        Task<bool> IsFrontDeskAssignedToPropertyAsync(int propertyId, string identityUserId, CancellationToken cancellationToken = default);
        Task<bool> IsEmployeeAssignedToPropertyAsync(int propertyId, string identityUserId, CancellationToken cancellationToken = default);
    }
}