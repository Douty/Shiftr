using System.Text.Json.Serialization;

namespace Shiftr.Models
{
    public enum AmenityDeleteResult
    {
        Deleted,
        NotFound,
        HasReservations
    }

    public enum AmenityReservationResult
    {
        Success,
        NotFound,
        AmenityNotFound,
        ResidentNotFound,
        InvalidTimeRange,
        TimeConflict
    }

    public class AmenityReservationModel
    {
        public int Id { get; set; }
        public int AmenityTypeId { get; set; }
        public required string ResidentIdentityUserId { get; set; }
        public DateTimeOffset StartsAt { get; set; }
        public DateTimeOffset EndsAt { get; set; }
        public string? Notes { get; set; }

        [JsonIgnore]
        public AmenityTypeModel? Amenity { get; set; }
    }
}