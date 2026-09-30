using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IOrganizationRepository
    {
        Task<OrganizationModel?> GetByIdAsync(int id);
        Task<OrganizationModel> AddAsync(OrganizationModel organization);
        Task<OrganizationModel?> UpdateAsync(OrganizationModel organization);
        Task<OrganizationDeleteResult> DeleteAsync(int id);
        Task<bool> AddEmployeeAsync(int organizationId, int employeeId, int? propertyId);
        Task<PropertyModel?> AddPropertyAsync(int organizationId, PropertyModel property);
        Task<bool> HasMemberAsync(int organizationId, string identityUserId);
        Task<bool> HasAdminAccessAsync(int organizationId, string identityUserId);
        Task<bool> HasOwnerAccessAsync(int organizationId, string identityUserId);
        Task<int?> GetOrganizationIdForEmployeeAsync(int employeeId);
        Task<int?> GetOrganizationIdForPropertyAsync(int propertyId);
    }
}