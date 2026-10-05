using Microsoft.EntityFrameworkCore;
using Shiftr.Data;
using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Repository
{
    public class AmenityRepository : IAmenityRepository
    {
        private readonly ShiftrDbContext _context;

        public AmenityRepository(ShiftrDbContext context)
        {
            _context = context;
        }

        public Task<bool> PropertyExistsAsync(int propertyId, CancellationToken cancellationToken = default) =>
            _context.Properties.AnyAsync(property => property.Id == propertyId, cancellationToken);

        public async Task<IReadOnlyList<AmenityTypeModel>> GetAmenitiesAsync(int propertyId, CancellationToken cancellationToken = default) =>
            await _context.Amenities
                .AsNoTracking()
                .Where(amenity => amenity.PropertyId == propertyId)
                .OrderBy(amenity => amenity.Name)
                .ToListAsync(cancellationToken);

        public Task<AmenityTypeModel?> GetAmenityAsync(int propertyId, int amenityId, CancellationToken cancellationToken = default) =>
            _context.Amenities.FirstOrDefaultAsync(
                amenity => amenity.PropertyId == propertyId && amenity.Id == amenityId,
                cancellationToken);

        public Task<bool> AmenityNameExistsAsync(int propertyId, string name, int? excludedAmenityId = null, CancellationToken cancellationToken = default) =>
            _context.Amenities.AnyAsync(
                amenity => amenity.PropertyId == propertyId && amenity.Name == name && amenity.Id != excludedAmenityId,
                cancellationToken);

        public async Task<AmenityTypeModel?> AddAmenityAsync(int propertyId, string name, string? description, CancellationToken cancellationToken = default)
        {
            if (!await PropertyExistsAsync(propertyId, cancellationToken)) return null;

            var amenity = new AmenityTypeModel
            {
                PropertyId = propertyId,
                Name = name,
                Description = description
            };
            _context.Amenities.Add(amenity);
            await _context.SaveChangesAsync(cancellationToken);
            return amenity;
        }

        public async Task<AmenityTypeModel?> UpdateAmenityAsync(int propertyId, int amenityId, string name, string? description, CancellationToken cancellationToken = default)
        {
            var amenity = await GetAmenityAsync(propertyId, amenityId, cancellationToken);
            if (amenity is null) return null;

            amenity.Name = name;
            amenity.Description = description;
            await _context.SaveChangesAsync(cancellationToken);
            return amenity;
        }

        public async Task<AmenityDeleteResult> DeleteAmenityAsync(int propertyId, int amenityId, CancellationToken cancellationToken = default)
        {
            var amenity = await _context.Amenities
                .Include(existing => existing.Reservations)
                .FirstOrDefaultAsync(
                    existing => existing.PropertyId == propertyId && existing.Id == amenityId,
                    cancellationToken);
            if (amenity is null) return AmenityDeleteResult.NotFound;
            if (amenity.Reservations.Count > 0) return AmenityDeleteResult.HasReservations;

            _context.Amenities.Remove(amenity);
            await _context.SaveChangesAsync(cancellationToken);
            return AmenityDeleteResult.Deleted;
        }

        public async Task<IReadOnlyList<AmenityReservationModel>> GetReservationsAsync(int propertyId, CancellationToken cancellationToken = default) =>
            await _context.AmenityReservations
                .AsNoTracking()
                .Where(reservation => reservation.Amenity!.PropertyId == propertyId)
                .OrderBy(reservation => reservation.StartsAt)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<AmenityReservationModel>> GetReservationsForResidentAsync(
            int propertyId,
            string residentIdentityUserId,
            CancellationToken cancellationToken = default) =>
            await _context.AmenityReservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.Amenity!.PropertyId == propertyId &&
                    reservation.ResidentIdentityUserId == residentIdentityUserId)
                .OrderBy(reservation => reservation.StartsAt)
                .ToListAsync(cancellationToken);

        public Task<AmenityReservationModel?> GetReservationAsync(int propertyId, int reservationId, CancellationToken cancellationToken = default) =>
            _context.AmenityReservations.FirstOrDefaultAsync(
                reservation => reservation.Id == reservationId && reservation.Amenity!.PropertyId == propertyId,
                cancellationToken);

        public Task<AmenityReservationModel?> GetReservationForResidentAsync(
            int propertyId,
            int reservationId,
            string residentIdentityUserId,
            CancellationToken cancellationToken = default) =>
            _context.AmenityReservations.FirstOrDefaultAsync(
                reservation =>
                    reservation.Id == reservationId &&
                    reservation.ResidentIdentityUserId == residentIdentityUserId &&
                    reservation.Amenity!.PropertyId == propertyId,
                cancellationToken);

        public Task<bool> AmenityExistsAsync(int propertyId, int amenityId, CancellationToken cancellationToken = default) =>
            _context.Amenities.AnyAsync(
                amenity => amenity.Id == amenityId && amenity.PropertyId == propertyId,
                cancellationToken);

        public Task<bool> ResidentExistsAsync(string identityUserId, CancellationToken cancellationToken = default) =>
            _context.Users.AnyAsync(user => user.Id == identityUserId, cancellationToken);

        public Task<bool> ReservationTimeConflictsAsync(int amenityId, DateTimeOffset startsAt, DateTimeOffset endsAt, int? excludedReservationId = null, CancellationToken cancellationToken = default) =>
            _context.AmenityReservations.AnyAsync(
                reservation => reservation.AmenityTypeId == amenityId &&
                    reservation.Id != excludedReservationId &&
                    reservation.StartsAt < endsAt && reservation.EndsAt > startsAt,
                cancellationToken);

        public async Task<AmenityReservationModel?> AddReservationAsync(int propertyId, int amenityId, string residentIdentityUserId, DateTimeOffset startsAt, DateTimeOffset endsAt, string? notes, CancellationToken cancellationToken = default)
        {
            if (!await AmenityExistsAsync(propertyId, amenityId, cancellationToken)) return null;

            var reservation = new AmenityReservationModel
            {
                AmenityTypeId = amenityId,
                ResidentIdentityUserId = residentIdentityUserId,
                StartsAt = startsAt,
                EndsAt = endsAt,
                Notes = notes
            };
            _context.AmenityReservations.Add(reservation);
            await _context.SaveChangesAsync(cancellationToken);
            return reservation;
        }

        public async Task<AmenityReservationModel?> UpdateReservationAsync(int propertyId, int reservationId, int amenityId, string residentIdentityUserId, DateTimeOffset startsAt, DateTimeOffset endsAt, string? notes, CancellationToken cancellationToken = default)
        {
            var reservation = await GetReservationAsync(propertyId, reservationId, cancellationToken);
            if (reservation is null || !await AmenityExistsAsync(propertyId, amenityId, cancellationToken)) return null;

            reservation.AmenityTypeId = amenityId;
            reservation.ResidentIdentityUserId = residentIdentityUserId;
            reservation.StartsAt = startsAt;
            reservation.EndsAt = endsAt;
            reservation.Notes = notes;
            await _context.SaveChangesAsync(cancellationToken);
            return reservation;
        }

        public async Task<AmenityReservationModel?> UpdateResidentReservationAsync(
            int propertyId,
            int reservationId,
            int amenityId,
            string residentIdentityUserId,
            DateTimeOffset startsAt,
            DateTimeOffset endsAt,
            string? notes,
            CancellationToken cancellationToken = default)
        {
            var reservation = await _context.AmenityReservations.FirstOrDefaultAsync(
                existing => existing.Id == reservationId &&
                    existing.ResidentIdentityUserId == residentIdentityUserId &&
                    existing.Amenity!.PropertyId == propertyId,
                cancellationToken);
            if (reservation is null || !await AmenityExistsAsync(propertyId, amenityId, cancellationToken)) return null;

            reservation.AmenityTypeId = amenityId;
            reservation.StartsAt = startsAt;
            reservation.EndsAt = endsAt;
            reservation.Notes = notes;
            await _context.SaveChangesAsync(cancellationToken);
            return reservation;
        }

        public async Task<bool> DeleteReservationAsync(int propertyId, int reservationId, CancellationToken cancellationToken = default)
        {
            var reservation = await GetReservationAsync(propertyId, reservationId, cancellationToken);
            if (reservation is null) return false;

            _context.AmenityReservations.Remove(reservation);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteResidentReservationAsync(
            int propertyId,
            int reservationId,
            string residentIdentityUserId,
            CancellationToken cancellationToken = default)
        {
            var reservation = await _context.AmenityReservations.FirstOrDefaultAsync(
                existing => existing.Id == reservationId &&
                    existing.ResidentIdentityUserId == residentIdentityUserId &&
                    existing.Amenity!.PropertyId == propertyId,
                cancellationToken);
            if (reservation is null) return false;

            _context.AmenityReservations.Remove(reservation);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}