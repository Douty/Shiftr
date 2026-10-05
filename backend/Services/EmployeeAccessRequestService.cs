using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shiftr.Data;
using Shiftr.DTOs;
using Shiftr.Models;
using Shiftr.Security;

namespace Shiftr.Services;

public enum EmployeeAccessRequestResult
{
    Success,
    NotFound,
    Conflict,
    InvalidAccount
}

public class EmployeeAccessRequestService
{
    private readonly ShiftrDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public EmployeeAccessRequestService(ShiftrDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Task<List<EmployeeAccessRequestModel>> GetPendingForOrganization(int organizationId) =>
        _context.Set<EmployeeAccessRequestModel>()
            .Where(request => request.Status == EmployeeAccessRequestStatus.Pending &&
                request.Property != null && EF.Property<int?>(request.Property, "OrganizationModelId") == organizationId)
            .Include(request => request.Property)
            .OrderBy(request => request.CreatedAt)
            .ToListAsync();

    public Task<EmployeeAccessRequestModel?> GetById(int requestId) =>
        _context.Set<EmployeeAccessRequestModel>()
            .Include(request => request.Property)
            .FirstOrDefaultAsync(request => request.Id == requestId);

    public async Task<EmployeeAccessRequestResult> Create(
        string identityUserId,
        CreateEmployeeAccessRequestRequest request)
    {
        var user = await _userManager.FindByIdAsync(identityUserId);
        if (user?.Email is null) return EmployeeAccessRequestResult.InvalidAccount;

        var inviteCode = request.EmployeeInviteCode.Trim();
        var property = await _context.Properties
            .FirstOrDefaultAsync(existing => existing.EmployeeInviteId == inviteCode);
        if (property is null) return EmployeeAccessRequestResult.NotFound;

        if (await _context.Employees.AnyAsync(employee => employee.IdentityUserId == identityUserId) ||
            await _context.Set<EmployeeAccessRequestModel>().AnyAsync(existing =>
                existing.PropertyId == property.Id &&
                existing.IdentityUserId == identityUserId &&
                existing.Status == EmployeeAccessRequestStatus.Pending))
        {
            return EmployeeAccessRequestResult.Conflict;
        }

        _context.Add(new EmployeeAccessRequestModel
        {
            PropertyId = property.Id,
            IdentityUserId = identityUserId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = user.Email,
            PhoneNumber = request.PhoneNumber.Trim(),
            Role = request.Role
        });
        await _context.SaveChangesAsync();
        return EmployeeAccessRequestResult.Success;
    }

    public async Task<EmployeeAccessRequestResult> Approve(int requestId)
    {
        var request = await GetById(requestId);
        if (request?.Property is null) return EmployeeAccessRequestResult.NotFound;
        if (request.Status != EmployeeAccessRequestStatus.Pending ||
            await _context.Employees.AnyAsync(employee => employee.IdentityUserId == request.IdentityUserId))
        {
            return EmployeeAccessRequestResult.Conflict;
        }

        var user = await _userManager.FindByIdAsync(request.IdentityUserId);
        if (user is null) return EmployeeAccessRequestResult.InvalidAccount;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        EmployeeBase employee = request.Role switch
        {
            EmployeeAccessRole.Manager => new ManagerModel
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                IdentityUserId = request.IdentityUserId,
                HireDate = DateTime.UtcNow,
                PropteryId = request.PropertyId
            },
            EmployeeAccessRole.FrontDesk => new FrontDeskAgentModel
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                IdentityUserId = request.IdentityUserId,
                HireDate = DateTime.UtcNow,
                PropteryId = request.PropertyId
            },
            _ => throw new ArgumentOutOfRangeException(nameof(request.Role))
        };

        _context.Employees.Add(employee);
        var roleName = request.Role == EmployeeAccessRole.Manager ? IdentityRoles.Admin : IdentityRoles.FrontDesk;
        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return EmployeeAccessRequestResult.InvalidAccount;
            }
        }

        request.Status = EmployeeAccessRequestStatus.Approved;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return EmployeeAccessRequestResult.Success;
    }

    public async Task<EmployeeAccessRequestResult> Reject(int requestId)
    {
        var request = await _context.Set<EmployeeAccessRequestModel>()
            .FirstOrDefaultAsync(existing => existing.Id == requestId);
        if (request is null) return EmployeeAccessRequestResult.NotFound;
        if (request.Status != EmployeeAccessRequestStatus.Pending) return EmployeeAccessRequestResult.Conflict;

        request.Status = EmployeeAccessRequestStatus.Rejected;
        await _context.SaveChangesAsync();
        return EmployeeAccessRequestResult.Success;
    }

    public Task<EmployeeAccessRequestModel?> GetCurrentForIdentity(string identityUserId) =>
        _context.Set<EmployeeAccessRequestModel>()
            .Where(request => request.IdentityUserId == identityUserId &&
                request.Status != EmployeeAccessRequestStatus.Rejected)
            .Include(request => request.Property)
            .OrderByDescending(request => request.CreatedAt)
            .FirstOrDefaultAsync();
}