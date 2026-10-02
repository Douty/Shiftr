using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IResidentService
    {
        Task<IReadOnlyList<ResidentModel>> GetResidents(int propertyId, CancellationToken cancellationToken = default);
        Task<ResidentModel?> GetResident(int propertyId, int residentId, CancellationToken cancellationToken = default);
        Task<ResidentModel?> CreateResident(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default);
        Task<ResidentModel?> UpdateResident(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default);
        Task<ResidentModel?> AddAllowedGuest(int propertyId, int residentId, string guestName, CancellationToken cancellationToken = default);
        Task<ResidentModel?> RemoveAllowedGuest(int propertyId, int residentId, string guestName, CancellationToken cancellationToken = default);
        Task<ResidentModel?> SetCallToNotify(int propertyId, int residentId, bool callToNotify, CancellationToken cancellationToken = default);
        Task<bool> DeleteResident(int propertyId, int residentId, CancellationToken cancellationToken = default);
    }
}