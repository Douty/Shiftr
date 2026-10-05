using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shiftr.DTOs;
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Security;
using Shiftr.Services;

namespace Shiftr.Controllers;

[ApiController]
[Route("api/employee-access-requests")]
public class EmployeeAccessRequestsController : ControllerBase
{
    private readonly EmployeeAccessRequestService _service;
    private readonly IOrganizationService _organizationService;
    private readonly IPropertyService _propertyService;

    public EmployeeAccessRequestsController(
        EmployeeAccessRequestService service,
        IOrganizationService organizationService,
        IPropertyService propertyService)
    {
        _service = service;
        _organizationService = organizationService;
        _propertyService = propertyService;
    }

    [HttpGet("organizations/{organizationId:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<List<EmployeeAccessRequestResponse>>> GetPendingRequests(int organizationId)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();
        if (!await _organizationService.HasAdminAccess(organizationId, identityUserId)) return Forbid();

        var requests = await _service.GetPendingForOrganization(organizationId);
        return Ok(requests.Select(ToResponse).ToList());
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<EmployeeAccessRequestResponse>> GetMyPendingRequest()
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();
        var request = await _service.GetCurrentForIdentity(identityUserId);
        return request is null ? NotFound() : Ok(ToResponse(request));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<EmployeeAccessRequestResponse>> CreateRequest(
        CreateEmployeeAccessRequestRequest request)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();

        var result = await _service.Create(identityUserId, request);
        if (result == EmployeeAccessRequestResult.NotFound) return NotFound();
        if (result == EmployeeAccessRequestResult.Conflict) return Conflict();
        if (result == EmployeeAccessRequestResult.InvalidAccount) return BadRequest();

        var createdRequest = await _service.GetCurrentForIdentity(identityUserId);
        return Accepted(ToResponse(createdRequest!));
    }

    [HttpPost("{requestId:int}/approve")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Approve(int requestId)
    {
        var request = await _service.GetById(requestId);
        if (request is null) return NotFound();

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();
        if (request.Property is null ||
            !await _propertyService.CanManageProperty(request.PropertyId, identityUserId)) return Forbid();

        return ToActionResult(await _service.Approve(requestId));
    }

    [HttpPost("{requestId:int}/reject")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Reject(int requestId)
    {
        var request = await _service.GetById(requestId);
        if (request is null) return NotFound();

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();
        if (request.Property is null ||
            !await _propertyService.CanManageProperty(request.PropertyId, identityUserId)) return Forbid();

        return ToActionResult(await _service.Reject(requestId));
    }

    private static EmployeeAccessRequestResponse ToResponse(EmployeeAccessRequestModel request) => new(
        request.Id,
        request.PropertyId,
        request.Property?.Name ?? "Property",
        request.FirstName,
        request.LastName,
        request.Email,
        request.PhoneNumber,
        request.Role,
        request.Status,
        request.CreatedAt);

    private IActionResult ToActionResult(EmployeeAccessRequestResult result) => result switch
    {
        EmployeeAccessRequestResult.Success => NoContent(),
        EmployeeAccessRequestResult.NotFound => NotFound(),
        EmployeeAccessRequestResult.Conflict => Conflict(new ProblemDetails
        {
            Title = "Request cannot be completed",
            Detail = "This account already has employee access or the request is no longer pending."
        }),
        EmployeeAccessRequestResult.InvalidAccount => BadRequest(new ProblemDetails
        {
            Title = "Account unavailable",
            Detail = "Sign in with a valid account that has an email address."
        }),
        _ => throw new InvalidOperationException("Unknown employee access request result.")
    };
}