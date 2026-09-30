using Microsoft.EntityFrameworkCore;
using Shiftr.Data;
using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Repository
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly ShiftrDbContext _context;

        public OrganizationRepository(ShiftrDbContext context)
        {
            _context = context;
        }

        public async Task<OrganizationModel?> GetByIdAsync(int id)
        {
            return await _context.Organizations
                .Include(organization => organization.Owners)
                .Include(organization => organization.Properties)
                .FirstOrDefaultAsync(organization => organization.Id == id);
        }

        public async Task<OrganizationModel> AddAsync(OrganizationModel organization)
        {
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();
            return organization;
        }

        public async Task<OrganizationModel?> UpdateAsync(OrganizationModel organization)
        {
            var existingOrganization = await _context.Organizations
                .FirstOrDefaultAsync(existing => existing.Id == organization.Id);

            if (existingOrganization is null) return null;

            existingOrganization.Name = organization.Name;
            await _context.SaveChangesAsync();
            return existingOrganization;
        }

        public async Task<OrganizationDeleteResult> DeleteAsync(int id)
        {
            var organization = await _context.Organizations
                .Include(existing => existing.Properties)
                .Include(existing => existing.Owners)
                .FirstOrDefaultAsync(existing => existing.Id == id);

            if (organization is null) return OrganizationDeleteResult.NotFound;

            if (organization.Properties.Count > 0 || organization.Owners.Count > 0)
            {
                return OrganizationDeleteResult.HasDependents;
            }

            _context.Organizations.Remove(organization);
            await _context.SaveChangesAsync();
            return OrganizationDeleteResult.Deleted;
        }

        public async Task<bool> AddEmployeeAsync(int organizationId, int employeeId, int? propertyId)
        {
            var organization = await _context.Organizations
                .Include(existing => existing.Owners)
                .Include(existing => existing.Properties)
                .FirstOrDefaultAsync(existing => existing.Id == organizationId);
            var employee = await _context.Employees.FirstOrDefaultAsync(existing => existing.Id == employeeId);

            if (organization is null || employee is null) return false;

            switch (employee)
            {
                case OwnerModel owner:
                    owner.OrganizationID = organizationId;
                    owner.Organization = organization;
                    if (!organization.Owners.Any(existing => existing.Id == owner.Id))
                    {
                        organization.Owners.Add(owner);
                    }
                    break;
                case ManagerModel manager when propertyId.HasValue:
                    var managerProperty = organization.Properties
                        .FirstOrDefault(existing => existing.Id == propertyId.Value);
                    if (managerProperty is null) return false;
                    manager.PropteryId = managerProperty.Id;
                    manager.Proptery = managerProperty;
                    if (!managerProperty.Managers.Any(existing => existing.Id == manager.Id))
                    {
                        managerProperty.Managers.Add(manager);
                    }
                    break;
                case FrontDeskAgentModel agent when propertyId.HasValue:
                    var agentProperty = organization.Properties
                        .FirstOrDefault(existing => existing.Id == propertyId.Value);
                    if (agentProperty is null) return false;
                    agent.PropteryId = agentProperty.Id;
                    agent.Proptery = agentProperty;
                    if (!agentProperty.FrontDeskAgents.Any(existing => existing.Id == agent.Id))
                    {
                        agentProperty.FrontDeskAgents.Add(agent);
                    }
                    break;
                default:
                    return false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PropertyModel?> AddPropertyAsync(int organizationId, PropertyModel property)
        {
            var organization = await _context.Organizations
                .Include(existing => existing.Properties)
                .FirstOrDefaultAsync(existing => existing.Id == organizationId);

            if (organization is null) return null;

            organization.Properties.Add(property);
            await _context.SaveChangesAsync();
            return property;
        }

        public async Task<bool> HasMemberAsync(int organizationId, string identityUserId)
        {
            var organization = await GetOrganizationForAccessAsync(organizationId);
            return organization is not null && HasMember(organization, identityUserId);
        }

        public async Task<bool> HasAdminAccessAsync(int organizationId, string identityUserId)
        {
            var organization = await GetOrganizationForAccessAsync(organizationId);
            return organization is not null &&
                (HasOwnerAccess(organization, identityUserId) || organization.Properties
                    .SelectMany(property => property.Managers)
                    .Any(manager => manager.IdentityUserId == identityUserId));
        }

        public async Task<bool> HasOwnerAccessAsync(int organizationId, string identityUserId)
        {
            var organization = await GetOrganizationForAccessAsync(organizationId);
            return organization is not null && HasOwnerAccess(organization, identityUserId);
        }

        public async Task<int?> GetOrganizationIdForEmployeeAsync(int employeeId)
        {
            var ownerOrganizationId = await _context.Employees
                .OfType<OwnerModel>()
                .Where(owner => owner.Id == employeeId)
                .Select(owner => (int?)owner.OrganizationID)
                .FirstOrDefaultAsync();
            if (ownerOrganizationId.HasValue) return ownerOrganizationId;

            var propertyId = await _context.Employees
                .OfType<ManagerModel>()
                .Where(manager => manager.Id == employeeId)
                .Select(manager => (int?)manager.PropteryId)
                .FirstOrDefaultAsync();
            propertyId ??= await _context.Employees
                .OfType<FrontDeskAgentModel>()
                .Where(agent => agent.Id == employeeId)
                .Select(agent => (int?)agent.PropteryId)
                .FirstOrDefaultAsync();
            return propertyId.HasValue
                ? await GetOrganizationIdForPropertyAsync(propertyId.Value)
                : null;
        }

        public async Task<int?> GetOrganizationIdForPropertyAsync(int propertyId) =>
            await _context.Properties
                .Where(property => property.Id == propertyId)
                .Select(property => EF.Property<int?>(property, "OrganizationModelId"))
                .FirstOrDefaultAsync();

        private Task<OrganizationModel?> GetOrganizationForAccessAsync(int organizationId) =>
            _context.Organizations
                .Include(organization => organization.Owners)
                .Include(organization => organization.Properties)
                    .ThenInclude(property => property.Managers)
                .Include(organization => organization.Properties)
                    .ThenInclude(property => property.FrontDeskAgents)
                .FirstOrDefaultAsync(organization => organization.Id == organizationId);

        private static bool HasMember(OrganizationModel organization, string identityUserId) =>
            HasOwnerAccess(organization, identityUserId) || organization.Properties.Any(property =>
                property.Managers.Any(manager => manager.IdentityUserId == identityUserId) ||
                property.FrontDeskAgents.Any(agent => agent.IdentityUserId == identityUserId));

        private static bool HasOwnerAccess(OrganizationModel organization, string identityUserId) =>
            organization.Owners.Any(owner => owner.IdentityUserId == identityUserId);
    }
}