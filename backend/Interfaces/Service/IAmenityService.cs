using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IAmenityService
    {
        Task<bool> PropertyExists(int propertyId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AmenityTypeModel>> GetAmenities(int propertyId, CancellationToken cancellationToken = default);
        Task<AmenityTypeModel?> GetAmenity(int propertyId, int amenityId, CancellationToken cancellationToken = default);
        Task<(AmenityTypeModel? Amenity, bool NameConflict)> CreateAmenity(int propertyId, string name, string? description, CancellationToken cancellationToken = default);
        Task<(AmenityTypeModel? Amenity, bool NameConflict)> UpdateAmenity(int propertyId, int amenityId, string name, string? description, CancellationToken cancellationToken = default);
        Task<AmenityDeleteResult> DeleteAmenity(int propertyId, int amenityId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AmenityReservationModel>> GetReservations(int propertyId, CancellationToken cancellationToken = default);
        Task<AmenityReservationModel?> GetReservation(int propertyId, int reservationId, CancellationToken cancellationToken = default);
        Task<(AmenityReservationResult Result, AmenityReservationModel? Reservation)> CreateReservation(int propertyId, int amenityId, string residentIdentityUserId, DateTimeOffset startsAt, DateTimeOffset endsAt, string? notes, CancellationToken cancellationToken = default);
        Task<(AmenityReservationResult Result, AmenityReservationModel? Reservation)> UpdateReservation(int propertyId, int reservationId, int amenityId, string residentIdentityUserId, DateTimeOffset startsAt, DateTimeOffset endsAt, string? notes, CancellationToken cancellationToken = default);
        Task<bool> DeleteReservation(int propertyId, int reservationId, CancellationToken cancellationToken = default);
    }
}