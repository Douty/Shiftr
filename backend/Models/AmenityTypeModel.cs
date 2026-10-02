using System.Text.Json.Serialization;

namespace Shiftr.Models
{
    public class AmenityTypeModel
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }

        [JsonIgnore]
        public PropertyModel? Property { get; set; }

        [JsonIgnore]
        public List<AmenityReservationModel> Reservations { get; set; } = new();
    }
}