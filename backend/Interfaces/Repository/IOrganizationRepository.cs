using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IOrganizationRepository
    {
        Task<OrganizationModel?> GetByIdAsync(int id);
        Task<OrganizationModel> AddAsync(OrganizationModel organization);
        Task<OrganizationModel?> UpdateAsync(OrganizationModel organization);
        Task<bool> DeleteAsync(int id);
        Task<bool> AddEmployeeAsync(int organizationId, int employeeId, int? propertyId);
        Task<PropertyModel?> AddPropertyAsync(int organizationId, PropertyModel property);
    }
}