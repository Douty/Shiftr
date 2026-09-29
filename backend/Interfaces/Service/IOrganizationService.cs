using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IOrganizationService
    {
        Task<OrganizationModel?> GetOrganizationById(int id);
        Task<OrganizationModel> CreateOrganization(OrganizationModel organization);
        Task<OrganizationModel?> UpdateOrganization(OrganizationModel organization);
        Task<bool> DeleteOrganization(int id);
        Task<bool> AddEmployeeToOrganization(int organizationId, int employeeId, int? propertyId);
        Task<PropertyModel?> AddPropertyToOrganization(int organizationId, PropertyModel property);
    }
}