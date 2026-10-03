namespace Shiftr.DTOs
{
    public record ShiftNoteResponse(
        int Id,
        string Title,
        string Content,
        int AuthorEmployeeId,
        string AuthorName,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}