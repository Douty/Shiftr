using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IOrganizationRepository _repository;

        public OrganizationService(IOrganizationRepository repository)
        {
            _repository = repository;
        }

        public Task<OrganizationModel?> GetOrganizationById(int id) =>
            _repository.GetByIdAsync(id);

        public Task<OrganizationModel> CreateOrganization(OrganizationModel organization) =>
            _repository.AddAsync(organization);

        public Task<OrganizationModel?> UpdateOrganization(OrganizationModel organization) =>
            _repository.UpdateAsync(organization);

        public Task<OrganizationDeleteResult> DeleteOrganization(int id) =>
            _repository.DeleteAsync(id);

        public Task<bool> AddEmployeeToOrganization(int organizationId, int employeeId, int? propertyId) =>
            _repository.AddEmployeeAsync(organizationId, employeeId, propertyId);

        public Task<PropertyModel?> AddPropertyToOrganization(int organizationId, PropertyModel property) =>
            _repository.AddPropertyAsync(organizationId, property);

        public Task<bool> HasMember(int organizationId, string identityUserId) =>
            _repository.HasMemberAsync(organizationId, identityUserId);

        public Task<bool> HasAdminAccess(int organizationId, string identityUserId) =>
            _repository.HasAdminAccessAsync(organizationId, identityUserId);

        public Task<bool> HasOwnerAccess(int organizationId, string identityUserId) =>
            _repository.HasOwnerAccessAsync(organizationId, identityUserId);
    }
}