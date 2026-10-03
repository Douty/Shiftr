using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IShiftNoteRepository
    {
        Task<(int EmployeeId, int PropertyId)?> GetFrontDeskAssignmentAsync(string identityUserId, CancellationToken cancellationToken);
        Task<IReadOnlyList<ShiftNoteModel>> GetByPropertyAsync(int propertyId, CancellationToken cancellationToken);
        Task<ShiftNoteModel?> GetByIdAsync(int propertyId, int noteId, CancellationToken cancellationToken);
        Task<ShiftNoteModel> AddAsync(ShiftNoteModel note, CancellationToken cancellationToken);
        Task<ShiftNoteModel?> UpdateAsync(ShiftNoteModel note, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(ShiftNoteModel note, CancellationToken cancellationToken);
    }
}