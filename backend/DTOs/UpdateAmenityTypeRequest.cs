using System.ComponentModel.DataAnnotations;

namespace Shiftr.DTOs
{
    /// <summary>Payload for updating a property amenity type.</summary>
    public sealed record UpdateAmenityTypeRequest
    {
        [Required, MaxLength(100)]
        public required string Name { get; init; }

        [MaxLength(500)]
        public string? Description { get; init; }
    }
}