using Microsoft.EntityFrameworkCore;
using Shiftr.Data;
using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Repository
{
    public class ResidentRepository : IResidentRepository
    {
        private readonly ShiftrDbContext _context;

        public ResidentRepository(ShiftrDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ResidentModel>> GetByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default) =>
            await _context.Residents
                .Where(resident => resident.PropertyId == propertyId)
                .OrderBy(resident => resident.LastName)
                .ThenBy(resident => resident.FirstName)
                .ToListAsync(cancellationToken);

        public Task<ResidentModel?> GetByIdAsync(int propertyId, int residentId, CancellationToken cancellationToken = default) =>
            _context.Residents.FirstOrDefaultAsync(
                resident => resident.PropertyId == propertyId && resident.Id == residentId,
                cancellationToken);

        public async Task<ResidentModel?> AddAsync(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default)
        {
            if (!await _context.Properties.AnyAsync(property => property.Id == propertyId, cancellationToken)) return null;

            resident.PropertyId = propertyId;
            _context.Residents.Add(resident);
            await _context.SaveChangesAsync(cancellationToken);
            return resident;
        }

        public async Task<ResidentModel?> UpdateAsync(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default)
        {
            var existingResident = await GetByIdAsync(propertyId, resident.Id, cancellationToken);
            if (existingResident is null) return null;

            existingResident.FirstName = resident.FirstName;
            existingResident.LastName = resident.LastName;
            existingResident.AllowedGuests = resident.AllowedGuests;
            existingResident.CallToNotify = resident.CallToNotify;
            await _context.SaveChangesAsync(cancellationToken);
            return existingResident;
        }

        public async Task<bool> DeleteAsync(int propertyId, int residentId, CancellationToken cancellationToken = default)
        {
            var resident = await GetByIdAsync(propertyId, residentId, cancellationToken);
            if (resident is null) return false;

            _context.Residents.Remove(resident);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}