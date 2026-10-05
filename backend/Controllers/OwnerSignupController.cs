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
[Route("api/owner-signup")]
public class OwnerSignupController : ControllerBase
{
    private readonly ShiftrDbContext _database;
    private readonly UserManager<IdentityUser> _userManager;

    public OwnerSignupController(ShiftrDbContext database, UserManager<IdentityUser> userManager)
    {
        _database = database;
        _userManager = userManager;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrganization(CreateOwnerSignupRequest request)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(identityUserId);
        if (user?.Email is null) return Unauthorized();

        if (await _database.Employees.AnyAsync(employee => employee.IdentityUserId == identityUserId) ||
            await _database.Residents.AnyAsync(resident => resident.IdentityUserId == identityUserId) ||
            await _userManager.IsInRoleAsync(user, IdentityRoles.Owner) ||
            await _userManager.IsInRoleAsync(user, IdentityRoles.Admin) ||
            await _userManager.IsInRoleAsync(user, IdentityRoles.Resident))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Account already has an organization role",
                Detail = "Use an account that does not already have employee or resident access."
            });
        }

        var organizationName = request.OrganizationName.Trim();
        if (string.IsNullOrWhiteSpace(organizationName))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Organization name is required",
                Detail = "Enter a name for your organization."
            });
        }

        await using var transaction = await _database.Database.BeginTransactionAsync();
        var organization = new OrganizationModel { Name = organizationName };
        _database.Organizations.Add(organization);
        await _database.SaveChangesAsync();

        _database.Employees.Add(new OwnerModel
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = user.Email,
            PhoneNumber = request.PhoneNumber.Trim(),
            IdentityUserId = identityUserId,
            HireDate = DateTime.UtcNow,
            OrganizationID = organization.Id
        });
        await _database.SaveChangesAsync();

        var roleResult = await _userManager.AddToRoleAsync(user, IdentityRoles.Owner);
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            var errors = string.Join(" ", roleResult.Errors.Select(error => error.Description));
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to assign organization owner access",
                detail: errors);
        }

        await transaction.CommitAsync();
        return Created($"/api/Organization/{organization.Id}", new
        {
            organization.Id,
            organization.Name
        });
    }
}
