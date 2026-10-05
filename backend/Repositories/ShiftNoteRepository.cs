using Microsoft.EntityFrameworkCore;
using Shiftr.Data;
using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Repository
{
    public class ShiftNoteRepository : IShiftNoteRepository
    {
        private readonly ShiftrDbContext _context;

        public ShiftNoteRepository(ShiftrDbContext context)
        {
            _context = context;
        }

        public async Task<(int EmployeeId, int PropertyId)?> GetEmployeeAssignmentAsync(string identityUserId, CancellationToken cancellationToken)
        {
            var managerAssignment = await _context.Employees
                .OfType<ManagerModel>()
                .Where(employee => employee.IdentityUserId == identityUserId)
                .Select(employee => new { employee.Id, employee.PropteryId })
                .FirstOrDefaultAsync(cancellationToken);
            if (managerAssignment is not null)
            {
                return (managerAssignment.Id, managerAssignment.PropteryId);
            }

            var frontDeskAssignment = await _context.Employees
                .OfType<FrontDeskAgentModel>()
                .Where(employee => employee.IdentityUserId == identityUserId)
                .Select(employee => new { employee.Id, employee.PropteryId })
                .FirstOrDefaultAsync(cancellationToken);
            return frontDeskAssignment is null
                ? null
                : (frontDeskAssignment.Id, frontDeskAssignment.PropteryId);
        }

        public async Task<IReadOnlyList<ShiftNoteModel>> GetByPropertyAsync(int propertyId, CancellationToken cancellationToken) =>
            await _context.ShiftNotes
                .AsNoTracking()
                .Include(note => note.AuthorEmployee)
                .Where(note => note.PropertyId == propertyId)
                .OrderByDescending(note => note.UpdatedAt)
                .ToListAsync(cancellationToken);

        public Task<ShiftNoteModel?> GetByIdAsync(int propertyId, int noteId, CancellationToken cancellationToken) =>
            _context.ShiftNotes
                .Include(note => note.AuthorEmployee)
                .FirstOrDefaultAsync(note => note.PropertyId == propertyId && note.Id == noteId, cancellationToken);

        public async Task<ShiftNoteModel> AddAsync(ShiftNoteModel note, CancellationToken cancellationToken)
        {
            _context.ShiftNotes.Add(note);
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Entry(note)
                .Reference(existing => existing.AuthorEmployee)
                .LoadAsync(cancellationToken);
            return note;
        }

        public async Task<ShiftNoteModel?> UpdateAsync(ShiftNoteModel note, CancellationToken cancellationToken)
        {
            var existingNote = await GetByIdAsync(note.PropertyId, note.Id, cancellationToken);
            if (existingNote is null) return null;

            existingNote.Title = note.Title;
            existingNote.Content = note.Content;
            existingNote.UpdatedAt = note.UpdatedAt;
            await _context.SaveChangesAsync(cancellationToken);
            return existingNote;
        }

        public async Task<bool> DeleteAsync(ShiftNoteModel note, CancellationToken cancellationToken)
        {
            _context.ShiftNotes.Remove(note);
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}