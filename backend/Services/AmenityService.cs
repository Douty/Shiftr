using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Services
{
    public class AmenityService : IAmenityService
    {
        private readonly IAmenityRepository _repository;

        public AmenityService(IAmenityRepository repository)
        {
            _repository = repository;
        }

        public Task<bool> PropertyExists(int propertyId, CancellationToken cancellationToken = default) =>
            _repository.PropertyExistsAsync(propertyId, cancellationToken);

        public Task<IReadOnlyList<AmenityTypeModel>> GetAmenities(int propertyId, CancellationToken cancellationToken = default) =>
            _repository.GetAmenitiesAsync(propertyId, cancellationToken);

        public Task<AmenityTypeModel?> GetAmenity(int propertyId, int amenityId, CancellationToken cancellationToken = default) =>
            _repository.GetAmenityAsync(propertyId, amenityId, cancellationToken);

        public async Task<(AmenityTypeModel? Amenity, bool NameConflict)> CreateAmenity(int propertyId, string name, string? description, CancellationToken cancellationToken = default)
        {
            name = name.Trim();
            if (await _repository.AmenityNameExistsAsync(propertyId, name, cancellationToken: cancellationToken))
                return (null, true);

            return (await _repository.AddAmenityAsync(propertyId, name, description?.Trim(), cancellationToken), false);
        }

        public async Task<(AmenityTypeModel? Amenity, bool NameConflict)> UpdateAmenity(int propertyId, int amenityId, string name, string? description, CancellationToken cancellationToken = default)
        {
            name = name.Trim();
            if (await _repository.GetAmenityAsync(propertyId, amenityId, cancellationToken) is null) return (null, false);
            if (await _repository.AmenityNameExistsAsync(propertyId, name, amenityId, cancellationToken)) return (null, true);

            return (await _repository.UpdateAmenityAsync(propertyId, amenityId, name, description?.Trim(), cancellationToken), false);
        }

        public Task<AmenityDeleteResult> DeleteAmenity(int propertyId, int amenityId, CancellationToken cancellationToken = default) =>
            _repository.DeleteAmenityAsync(propertyId, amenityId, cancellationToken);

        public Task<IReadOnlyList<AmenityReservationModel>> GetReservations(int propertyId, CancellationToken cancellationToken = default) =>
            _repository.GetReservationsAsync(propertyId, cancellationToken);

        public Task<AmenityReservationModel?> GetReservation(int propertyId, int reservationId, CancellationToken cancellationToken = default) =>
            _repository.GetReservationAsync(propertyId, reservationId, cancellationToken);

        public async Task<(AmenityReservationResult Result, AmenityReservationModel? Reservation)> CreateReservation(int propertyId, int amenityId, string residentIdentityUserId, DateTimeOffset startsAt, DateTimeOffset endsAt, string? notes, CancellationToken cancellationToken = default)
        {
            startsAt = startsAt.ToUniversalTime();
            endsAt = endsAt.ToUniversalTime();
            if (endsAt <= startsAt) return (AmenityReservationResult.InvalidTimeRange, null);
            if (!await _repository.AmenityExistsAsync(propertyId, amenityId, cancellationToken)) return (AmenityReservationResult.AmenityNotFound, null);
            if (!await _repository.ResidentExistsAsync(residentIdentityUserId, cancellationToken)) return (AmenityReservationResult.ResidentNotFound, null);
            if (await _repository.ReservationTimeConflictsAsync(amenityId, startsAt, endsAt, cancellationToken: cancellationToken))
                return (AmenityReservationResult.TimeConflict, null);

            var reservation = await _repository.AddReservationAsync(
                propertyId, amenityId, residentIdentityUserId, startsAt, endsAt, notes?.Trim(), cancellationToken);
            return reservation is null
                ? (AmenityReservationResult.AmenityNotFound, null)
                : (AmenityReservationResult.Success, reservation);
        }

        public async Task<(AmenityReservationResult Result, AmenityReservationModel? Reservation)> UpdateReservation(int propertyId, int reservationId, int amenityId, string residentIdentityUserId, DateTimeOffset startsAt, DateTimeOffset endsAt, string? notes, CancellationToken cancellationToken = default)
        {
            if (await _repository.GetReservationAsync(propertyId, reservationId, cancellationToken) is null)
                return (AmenityReservationResult.NotFound, null);
            startsAt = startsAt.ToUniversalTime();
            endsAt = endsAt.ToUniversalTime();
            if (endsAt <= startsAt) return (AmenityReservationResult.InvalidTimeRange, null);
            if (!await _repository.AmenityExistsAsync(propertyId, amenityId, cancellationToken)) return (AmenityReservationResult.AmenityNotFound, null);
            if (!await _repository.ResidentExistsAsync(residentIdentityUserId, cancellationToken)) return (AmenityReservationResult.ResidentNotFound, null);
            if (await _repository.ReservationTimeConflictsAsync(amenityId, startsAt, endsAt, reservationId, cancellationToken))
                return (AmenityReservationResult.TimeConflict, null);

            var reservation = await _repository.UpdateReservationAsync(
                propertyId, reservationId, amenityId, residentIdentityUserId, startsAt, endsAt, notes?.Trim(), cancellationToken);
            return reservation is null
                ? (AmenityReservationResult.NotFound, null)
                : (AmenityReservationResult.Success, reservation);
        }

        public Task<bool> DeleteReservation(int propertyId, int reservationId, CancellationToken cancellationToken = default) =>
            _repository.DeleteReservationAsync(propertyId, reservationId, cancellationToken);
    }
}