using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shiftr.Data;
using Shiftr.DTOs;
using Shiftr.Models;
using Shiftr.Security;

namespace Shiftr.Controllers;

[ApiController]
[Route("api/resident-signup")]
public sealed class ResidentSignupController : ControllerBase
{
    private readonly ShiftrDbContext _database;
    private readonly UserManager<IdentityUser> _userManager;

    public ResidentSignupController(ShiftrDbContext database, UserManager<IdentityUser> userManager)
    {
        _database = database;
        _userManager = userManager;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateResidentProfile(CreateResidentSignupRequest request, CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(identityUserId);
        if (user is null) return Unauthorized();

        var alreadyHasResidentRole = await _userManager.IsInRoleAsync(user, IdentityRoles.Resident);
        if (await _database.Employees.AnyAsync(employee => employee.IdentityUserId == identityUserId, cancellationToken) ||
            await _database.Residents.AnyAsync(resident => resident.IdentityUserId == identityUserId, cancellationToken) ||
            await _userManager.IsInRoleAsync(user, IdentityRoles.Owner) ||
            await _userManager.IsInRoleAsync(user, IdentityRoles.Admin) ||
            await _userManager.IsInRoleAsync(user, IdentityRoles.FrontDesk))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Account already has a building role",
                Detail = "Use an account that does not already have resident or employee access."
            });
        }

        var property = await _database.Properties.FirstOrDefaultAsync(
            item => item.ResidentInviteId == request.InviteCode.Trim(),
            cancellationToken);
        if (property is null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid resident invite code",
                Detail = "Enter an active resident invite code for your property."
            });
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var unitNumber = string.IsNullOrWhiteSpace(request.UnitNumber) ? null : request.UnitNumber.Trim();
        var possibleProfiles = await _database.Residents
            .Where(resident => resident.PropertyId == property.Id && resident.IdentityUserId == null)
            .ToListAsync(cancellationToken);
        var matches = possibleProfiles.Where(resident =>
            string.Equals(resident.FirstName, firstName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(resident.LastName, lastName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(resident.UnitNumber, unitNumber, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count > 1)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Resident profile needs confirmation",
                Detail = "Ask your property team to confirm your resident profile before creating an account."
            });
        }

        await using var transaction = await _database.Database.BeginTransactionAsync(cancellationToken);
        var resident = matches.SingleOrDefault() ?? new ResidentModel
        {
            PropertyId = property.Id,
            FirstName = firstName,
            LastName = lastName,
            UnitNumber = unitNumber
        };
        resident.IdentityUserId = identityUserId;
        if (matches.Count == 0) _database.Residents.Add(resident);
        await _database.SaveChangesAsync(cancellationToken);

        var roleResult = alreadyHasResidentRole
            ? IdentityResult.Success
            : await _userManager.AddToRoleAsync(user, IdentityRoles.Resident);
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            var errors = string.Join(" ", roleResult.Errors.Select(error => error.Description));
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to assign resident access",
                detail: errors);
        }

        await transaction.CommitAsync(cancellationToken);
        return Created("/api/resident/profile", new { resident.Id, resident.PropertyId });
    }
}
