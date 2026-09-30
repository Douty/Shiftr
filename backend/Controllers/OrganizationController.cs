using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Shiftr.DTOs;
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Security;

namespace Shiftr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationController : ControllerBase
    {
        
        private readonly IOrganizationService _service;

        public OrganizationController(IOrganizationService service)
        {
            _service = service;
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.RegularEmployee)]
        public async Task<ActionResult<OrganizationModel>> GetOrganization(int id)
        {
            var organization = await _service.GetOrganizationById(id);
            if (organization is null) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.HasMember(id, identityUserId)) return Forbid();
            return Ok(organization);
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
        public async Task<ActionResult<OrganizationModel>> CreateOrganization(OrganizationModel organization)
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null) return Unauthorized();
            if (!organization.Owners.Any(owner => owner.IdentityUserId == identityUserId)) return BadRequest();

            var createdOrganization = await _service.CreateOrganization(organization);
            return CreatedAtAction(nameof(GetOrganization), new { id = createdOrganization.Id }, createdOrganization);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
        public async Task<ActionResult<OrganizationModel>> UpdateOrganization(int id, OrganizationModel organization)
        {
            if (id != organization.Id) return BadRequest();
            if (await _service.GetOrganizationById(id) is null) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.HasOwnerAccess(id, identityUserId)) return Forbid();

            var updatedOrganization = await _service.UpdateOrganization(organization);
            return updatedOrganization is null ? NotFound() : Ok(updatedOrganization);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            if (await _service.GetOrganizationById(id) is null) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.HasOwnerAccess(id, identityUserId)) return Forbid();

            return await _service.DeleteOrganization(id) switch
            {
                OrganizationDeleteResult.Deleted => NoContent(),
                OrganizationDeleteResult.NotFound => NotFound(),
                OrganizationDeleteResult.HasDependents => Conflict(),
                _ => throw new InvalidOperationException("Unknown organization deletion result.")
            };
        }

        [HttpPost("{id:int}/employees")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> AddEmployee(int id, AddOrganizationEmployeeRequest request)
        {
            if (await _service.GetOrganizationById(id) is null) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.HasAdminAccess(id, identityUserId)) return Forbid();

            var added = await _service.AddEmployeeToOrganization(id, request.EmployeeId, request.PropertyId);
            return added ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/properties")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<ActionResult<PropertyModel>> AddProperty(int id, PropertyModel property)
        {
            if (await _service.GetOrganizationById(id) is null) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.HasAdminAccess(id, identityUserId)) return Forbid();

            var addedProperty = await _service.AddPropertyToOrganization(id, property);
            return addedProperty is null ? NotFound() : Ok(addedProperty);
        }

        
    }
}