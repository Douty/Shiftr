using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IOrganizationService
    {
        Task<OrganizationModel?> GetOrganizationById(int id);
        Task<List<OrganizationModel>> GetOrganizationsForUser(string identityUserId);
        Task<OrganizationModel> CreateOrganization(OrganizationModel organization);
        Task<OrganizationModel?> UpdateOrganization(OrganizationModel organization);
        Task<OrganizationDeleteResult> DeleteOrganization(int id);
        Task<bool> AddEmployeeToOrganization(int organizationId, int employeeId, int? propertyId);
        Task<PropertyModel?> AddPropertyToOrganization(int organizationId, PropertyModel property);
        Task<bool> HasMember(int organizationId, string identityUserId);
        Task<bool> HasAdminAccess(int organizationId, string identityUserId);
        Task<bool> HasOwnerAccess(int organizationId, string identityUserId);
    }
}