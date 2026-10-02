using System.ComponentModel.DataAnnotations;

namespace Shiftr.DTOs
{
    /// <summary>Payload for creating a resident amenity reservation.</summary>
    public sealed record CreateAmenityReservationRequest
    {
        [Range(1, int.MaxValue)]
        public required int AmenityTypeId { get; init; }

        [Required]
        public required string ResidentIdentityUserId { get; init; }

        public required DateTimeOffset StartsAt { get; init; }
        public required DateTimeOffset EndsAt { get; init; }

        [MaxLength(1000)]
        public string? Notes { get; init; }
    }
}