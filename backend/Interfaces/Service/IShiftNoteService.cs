using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IShiftNoteService
    {
        Task<(int EmployeeId, int PropertyId)?> GetFrontDeskAssignmentAsync(string identityUserId, CancellationToken cancellationToken);
        Task<IReadOnlyList<ShiftNoteModel>> GetNotesAsync(int propertyId, CancellationToken cancellationToken);
        Task<ShiftNoteModel?> GetNoteAsync(int propertyId, int noteId, CancellationToken cancellationToken);
        Task<ShiftNoteModel> CreateNoteAsync(int propertyId, int employeeId, string title, string content, CancellationToken cancellationToken);
        Task<ShiftNoteModel?> UpdateNoteAsync(int propertyId, int noteId, string title, string content, CancellationToken cancellationToken);
        Task<bool> DeleteNoteAsync(int propertyId, int noteId, CancellationToken cancellationToken);
    }
}