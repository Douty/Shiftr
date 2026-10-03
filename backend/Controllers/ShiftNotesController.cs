using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shiftr.DTOs;
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Security;
using System.Security.Claims;

namespace Shiftr.Controllers
{
    [ApiController]
    [Route("api/shift-notes")]
    [Authorize(Roles = IdentityRoles.FrontDesk)]
    public class ShiftNotesController : ControllerBase
    {
        private readonly IShiftNoteService _service;

        public ShiftNotesController(IShiftNoteService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotes(CancellationToken cancellationToken)
        {
            var assignment = await GetAssignment(cancellationToken);
            if (assignment is null) return Forbid();

            var notes = await _service.GetNotesAsync(assignment.Value.PropertyId, cancellationToken);
            return Ok(notes.Select(ToResponse));
        }

        [HttpGet("{noteId:int}")]
        public async Task<IActionResult> GetNote(int noteId, CancellationToken cancellationToken)
        {
            var assignment = await GetAssignment(cancellationToken);
            if (assignment is null) return Forbid();

            var note = await _service.GetNoteAsync(assignment.Value.PropertyId, noteId, cancellationToken);
            return note is null ? NotFound() : Ok(ToResponse(note));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNote(SaveShiftNoteRequest request, CancellationToken cancellationToken)
        {
            var assignment = await GetAssignment(cancellationToken);
            if (assignment is null) return Forbid();

            var note = await _service.CreateNoteAsync(
                assignment.Value.PropertyId, assignment.Value.EmployeeId, request.Title, request.Content, cancellationToken);
            return CreatedAtAction(nameof(GetNote), new { noteId = note.Id }, ToResponse(note));
        }

        [HttpPut("{noteId:int}")]
        public async Task<IActionResult> UpdateNote(int noteId, SaveShiftNoteRequest request, CancellationToken cancellationToken)
        {
            var assignment = await GetAssignment(cancellationToken);
            if (assignment is null) return Forbid();

            var note = await _service.UpdateNoteAsync(
                assignment.Value.PropertyId, noteId, request.Title, request.Content, cancellationToken);
            return note is null ? NotFound() : Ok(ToResponse(note));
        }

        [HttpDelete("{noteId:int}")]
        public async Task<IActionResult> DeleteNote(int noteId, CancellationToken cancellationToken)
        {
            var assignment = await GetAssignment(cancellationToken);
            if (assignment is null) return Forbid();

            return await _service.DeleteNoteAsync(assignment.Value.PropertyId, noteId, cancellationToken)
                ? NoContent()
                : NotFound();
        }

        private Task<(int EmployeeId, int PropertyId)?> GetAssignment(CancellationToken cancellationToken)
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return identityUserId is null
                ? Task.FromResult<(int EmployeeId, int PropertyId)?>(null)
                : _service.GetFrontDeskAssignmentAsync(identityUserId, cancellationToken);
        }

        private static ShiftNoteResponse ToResponse(ShiftNoteModel note) =>
            new(note.Id, note.Title, note.Content, note.AuthorEmployeeId,
                note.AuthorEmployee?.FullName ?? "Former employee", note.CreatedAt, note.UpdatedAt);
    }
}