using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IPropertyService
    {
        Task<PropertyModel?> GetPropertyById(int id);
        Task<PropertyModel?> CreateProperty(int organizationId, PropertyModel property);
        Task<PropertyModel?> UpdateProperty(PropertyModel property);
        Task<bool> IsInviteCodeValidAsync(string inviteCode, CancellationToken cancellationToken = default);
        Task<string?> RotateInviteId(int id, PropertyInviteType inviteType);
        Task<PropertyDeleteResult> DeleteProperty(int id);
        Task<bool> CanAccessProperty(int propertyId, string identityUserId);
        Task<bool> CanManageProperty(int propertyId, string identityUserId);
        Task<bool> CanManageOrganization(int organizationId, string identityUserId);
        Task<bool> CanManageReservations(int propertyId, string identityUserId, CancellationToken cancellationToken = default);
    }
}