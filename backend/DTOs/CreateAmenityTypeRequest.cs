using System.ComponentModel.DataAnnotations;

namespace Shiftr.DTOs
{
    /// <summary>Payload for creating a property amenity type.</summary>
    public sealed record CreateAmenityTypeRequest
    {
        [Required, MaxLength(100)]
        public required string Name { get; init; }

        [MaxLength(500)]
        public string? Description { get; init; }
    }
}