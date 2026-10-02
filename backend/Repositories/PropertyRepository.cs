using Microsoft.EntityFrameworkCore;
using Shiftr.Data;
using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Repository
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly ShiftrDbContext _context;

        public PropertyRepository(ShiftrDbContext context)
        {
            _context = context;
        }

        public Task<PropertyModel?> GetByIdAsync(int id) =>
            _context.Properties.FirstOrDefaultAsync(property => property.Id == id);

        public Task<bool> IsInviteCodeValidAsync(string inviteCode, CancellationToken cancellationToken = default) =>
            _context.Properties.AnyAsync(
                property => property.ResidentInviteId == inviteCode || property.EmployeeInviteId == inviteCode,
                cancellationToken);

        public async Task<PropertyModel?> AddAsync(int organizationId, PropertyModel property)
        {
            var organization = await _context.Organizations
                .Include(existing => existing.Properties)
                .FirstOrDefaultAsync(existing => existing.Id == organizationId);

            if (organization is null) return null;

            organization.Properties.Add(property);
            await _context.SaveChangesAsync();
            return property;
        }

        public async Task<PropertyModel?> UpdateAsync(PropertyModel property)
        {
            var existingProperty = await _context.Properties
                .FirstOrDefaultAsync(existing => existing.Id == property.Id);

            if (existingProperty is null) return null;

            existingProperty.Name = property.Name;
            await _context.SaveChangesAsync();
            return existingProperty;
        }

        public async Task<string?> RotateInviteIdAsync(int id, PropertyInviteType inviteType)
        {
            var property = await _context.Properties.FirstOrDefaultAsync(existing => existing.Id == id);
            if (property is null) return null;

            var inviteId = property.RotateInviteId(inviteType);
            await _context.SaveChangesAsync();
            return inviteId;
        }

        public async Task<PropertyDeleteResult> DeleteAsync(int id)
        {
            var property = await _context.Properties
                .Include(existing => existing.Managers)
                .Include(existing => existing.FrontDeskAgents)
                .Include(existing => existing.Amenities)
                    .ThenInclude(amenity => amenity.Reservations)
                .FirstOrDefaultAsync(existing => existing.Id == id);

            if (property is null) return PropertyDeleteResult.NotFound;
            if (property.Managers.Count > 0 || property.FrontDeskAgents.Count > 0)
            {
                return PropertyDeleteResult.HasEmployees;
            }
            if (property.Amenities.Any(amenity => amenity.Reservations.Count > 0))
            {
                return PropertyDeleteResult.HasReservations;
            }

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();
            return PropertyDeleteResult.Deleted;
        }

        public Task<bool> IsFrontDeskAssignedToPropertyAsync(int propertyId, string identityUserId, CancellationToken cancellationToken = default) =>
            _context.Properties.AnyAsync(property =>
                property.Id == propertyId &&
            property.FrontDeskAgents.Any(agent => agent.IdentityUserId == identityUserId),
            cancellationToken);
    }
}