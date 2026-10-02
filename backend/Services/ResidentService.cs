using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Services
{
    public class ResidentService : IResidentService
    {
        private readonly IResidentRepository _repository;

        public ResidentService(IResidentRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyList<ResidentModel>> GetResidents(int propertyId, CancellationToken cancellationToken = default) =>
            _repository.GetByPropertyIdAsync(propertyId, cancellationToken);

        public Task<ResidentModel?> GetResident(int propertyId, int residentId, CancellationToken cancellationToken = default) =>
            _repository.GetByIdAsync(propertyId, residentId, cancellationToken);

        public Task<ResidentModel?> CreateResident(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default) =>
            _repository.AddAsync(propertyId, resident, cancellationToken);

        public Task<ResidentModel?> UpdateResident(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default) =>
            _repository.UpdateAsync(propertyId, resident, cancellationToken);

        public async Task<ResidentModel?> AddAllowedGuest(int propertyId, int residentId, string guestName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(guestName)) return null;

            var resident = await _repository.GetByIdAsync(propertyId, residentId, cancellationToken);
            if (resident is null) return null;

            guestName = guestName.Trim();
            if (!resident.AllowedGuests.Contains(guestName, StringComparer.OrdinalIgnoreCase))
                resident.AllowedGuests.Add(guestName);

            return await _repository.UpdateAsync(propertyId, resident, cancellationToken);
        }

        public async Task<ResidentModel?> RemoveAllowedGuest(int propertyId, int residentId, string guestName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(guestName)) return null;

            var resident = await _repository.GetByIdAsync(propertyId, residentId, cancellationToken);
            if (resident is null) return null;

            resident.AllowedGuests.RemoveAll(guest => string.Equals(guest, guestName.Trim(), StringComparison.OrdinalIgnoreCase));
            return await _repository.UpdateAsync(propertyId, resident, cancellationToken);
        }

        public async Task<ResidentModel?> SetCallToNotify(int propertyId, int residentId, bool callToNotify, CancellationToken cancellationToken = default)
        {
            var resident = await _repository.GetByIdAsync(propertyId, residentId, cancellationToken);
            if (resident is null) return null;

            resident.CallToNotify = callToNotify;
            return await _repository.UpdateAsync(propertyId, resident, cancellationToken);
        }

        public Task<bool> DeleteResident(int propertyId, int residentId, CancellationToken cancellationToken = default) =>
            _repository.DeleteAsync(propertyId, residentId, cancellationToken);
    }
}