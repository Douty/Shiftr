using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IResidentRepository
    {
        Task<IReadOnlyList<ResidentModel>> GetByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<ResidentModel?> GetByIdAsync(int propertyId, int residentId, CancellationToken cancellationToken = default);
        Task<ResidentModel?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default);
        Task<ResidentModel?> UpdatePreferencesAsync(string identityUserId, bool callToNotify, List<string> allowedGuests, CancellationToken cancellationToken = default);
        Task<ResidentModel?> AddAsync(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default);
        Task<ResidentModel?> UpdateAsync(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int propertyId, int residentId, CancellationToken cancellationToken = default);
    }
}