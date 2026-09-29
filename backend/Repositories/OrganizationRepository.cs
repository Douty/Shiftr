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

        public async Task<bool> DeleteAsync(int id)
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(existing => existing.Id == id);

            if (organization is null) return false;

            _context.Organizations.Remove(organization);
            return await _context.SaveChangesAsync() > 0;
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
    }
}