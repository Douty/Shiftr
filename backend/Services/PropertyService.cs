using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly IPropertyRepository _repository;
        private readonly IOrganizationRepository _organizationRepository;

        public PropertyService(
            IPropertyRepository repository,
            IOrganizationRepository organizationRepository)
        {
            _repository = repository;
            _organizationRepository = organizationRepository;
        }

        public Task<PropertyModel?> GetPropertyById(int id) =>
            _repository.GetByIdAsync(id);

        public Task<PropertyModel?> CreateProperty(int organizationId, PropertyModel property) =>
            _repository.AddAsync(organizationId, property);

        public Task<PropertyModel?> UpdateProperty(PropertyModel property) =>
            _repository.UpdateAsync(property);

        public Task<bool> IsInviteCodeValidAsync(string inviteCode, CancellationToken cancellationToken = default) =>
            _repository.IsInviteCodeValidAsync(inviteCode, cancellationToken);

        public Task<string?> RotateInviteId(int id, PropertyInviteType inviteType) =>
            _repository.RotateInviteIdAsync(id, inviteType);

        public Task<PropertyDeleteResult> DeleteProperty(int id) =>
            _repository.DeleteAsync(id);

        public async Task<bool> CanAccessProperty(int propertyId, string identityUserId)
        {
            var organizationId = await _organizationRepository.GetOrganizationIdForPropertyAsync(propertyId);
            return organizationId.HasValue &&
                await _organizationRepository.HasMemberAsync(organizationId.Value, identityUserId);
        }

        public Task<bool> CanAccessAssignedProperty(int propertyId, string identityUserId, CancellationToken cancellationToken = default) =>
            _repository.IsEmployeeAssignedToPropertyAsync(propertyId, identityUserId, cancellationToken);

        public async Task<bool> CanManageProperty(int propertyId, string identityUserId)
        {
            var organizationId = await _organizationRepository.GetOrganizationIdForPropertyAsync(propertyId);
            return organizationId.HasValue &&
                await _organizationRepository.HasAdminAccessAsync(organizationId.Value, identityUserId);
        }

        public Task<bool> CanManageOrganization(int organizationId, string identityUserId) =>
            _organizationRepository.HasAdminAccessAsync(organizationId, identityUserId);

        public Task<bool> CanManageReservations(int propertyId, string identityUserId, CancellationToken cancellationToken = default) =>
            _repository.IsFrontDeskAssignedToPropertyAsync(propertyId, identityUserId, cancellationToken);
    }
}