using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Services
{
    public class ShiftNoteService : IShiftNoteService
    {
        private readonly IShiftNoteRepository _repository;

        public ShiftNoteService(IShiftNoteRepository repository)
        {
            _repository = repository;
        }

        public Task<(int EmployeeId, int PropertyId)?> GetFrontDeskAssignmentAsync(string identityUserId, CancellationToken cancellationToken) =>
            _repository.GetFrontDeskAssignmentAsync(identityUserId, cancellationToken);

        public Task<IReadOnlyList<ShiftNoteModel>> GetNotesAsync(int propertyId, CancellationToken cancellationToken) =>
            _repository.GetByPropertyAsync(propertyId, cancellationToken);

        public Task<ShiftNoteModel?> GetNoteAsync(int propertyId, int noteId, CancellationToken cancellationToken) =>
            _repository.GetByIdAsync(propertyId, noteId, cancellationToken);

        public Task<ShiftNoteModel> CreateNoteAsync(int propertyId, int employeeId, string title, string content, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            return _repository.AddAsync(new ShiftNoteModel
            {
                PropertyId = propertyId,
                AuthorEmployeeId = employeeId,
                Title = title.Trim(),
                Content = content.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);
        }

        public Task<ShiftNoteModel?> UpdateNoteAsync(int propertyId, int noteId, string title, string content, CancellationToken cancellationToken) =>
            _repository.UpdateAsync(new ShiftNoteModel
            {
                Id = noteId,
                PropertyId = propertyId,
                AuthorEmployeeId = 0,
                Title = title.Trim(),
                Content = content.Trim(),
                CreatedAt = default,
                UpdatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);

        public async Task<bool> DeleteNoteAsync(int propertyId, int noteId, CancellationToken cancellationToken)
        {
            var note = await _repository.GetByIdAsync(propertyId, noteId, cancellationToken);
            return note is not null && await _repository.DeleteAsync(note, cancellationToken);
        }
    }
}