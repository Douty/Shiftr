using System.Text.Json.Serialization;

namespace Shiftr.Models
{
    public class ShiftNoteModel
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public int AuthorEmployeeId { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonIgnore]
        public EmployeeBase? AuthorEmployee { get; set; }
    }
}