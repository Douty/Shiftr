using System.ComponentModel.DataAnnotations;

namespace Shiftr.DTOs
{
    public class SaveShiftNoteRequest
    {
        [Required, StringLength(120, MinimumLength = 1)]
        public required string Title { get; set; }

        [Required, StringLength(10000, MinimumLength = 1)]
        public required string Content { get; set; }
    }
}