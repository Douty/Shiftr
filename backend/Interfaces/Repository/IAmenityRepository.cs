using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IAmenityRepository
    {
        Task<bool> PropertyExistsAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AmenityTypeModel>> GetAmenitiesAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<AmenityTypeModel?> GetAmenityAsync(int propertyId, int amenityId, CancellationToken cancellationToken = default);
        Task<bool> AmenityNameExistsAsync(int propertyId, string name, int? excludedAmenityId = null, CancellationToken cancellationToken = default);
        Task<AmenityTypeModel?> AddAmenityAsync(int propertyId, string name, string? description, CancellationToken cancellationToken = default);
        Task<AmenityTypeModel?> UpdateAmenityAsync(int propertyId, int amenityId, string name, string? description, CancellationToken cancellationToken = default);
        Task<AmenityDeleteResult> DeleteAmenityAsync(int propertyId, int amenityId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AmenityReservationModel>> GetReservationsAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<AmenityReservationModel?> GetReservationAsync(int propertyId, int reservationId, CancellationToken cancellationToken = default);
        Task<bool> AmenityExistsAsync(int propertyId, int amenityId, CancellationToken cancellationToken = default);
        Task<bool> ResidentExistsAsync(string identityUserId, CancellationToken cancellationToken = default);
        Task<bool> ReservationTimeConflictsAsync(int amenityId, DateTimeOffset startsAt, DateTimeOffset endsAt, int? excludedReservationId = null, CancellationToken cancellationToken = default);
        Task<AmenityReservationModel?> AddReservationAsync(int propertyId, int amenityId, string residentIdentityUserId, DateTimeOffset startsAt, DateTimeOffset endsAt, string? notes, CancellationToken cancellationToken = default);
        Task<AmenityReservationModel?> UpdateReservationAsync(int propertyId, int reservationId, int amenityId, string residentIdentityUserId, DateTimeOffset startsAt, DateTimeOffset endsAt, string? notes, CancellationToken cancellationToken = default);
        Task<bool> DeleteReservationAsync(int propertyId, int reservationId, CancellationToken cancellationToken = default);
    }
}