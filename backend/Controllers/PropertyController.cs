using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Security;

namespace Shiftr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyController : ControllerBase
    {
        private readonly IPropertyService _service;

        public PropertyController(IPropertyService service)
        {
            _service = service;
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.RegularEmployee)]
        public async Task<ActionResult<PropertyModel>> GetProperty(int id)
        {
            var property = await _service.GetPropertyById(id);
            if (property is null) return NotFound();

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.CanAccessProperty(id, identityUserId)) return Forbid();
            return Ok(property);
        }

        [HttpPost("organizations/{organizationId:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<ActionResult<PropertyModel>> CreateProperty(int organizationId, PropertyModel property)
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.CanManageOrganization(organizationId, identityUserId)) return Forbid();

            var createdProperty = await _service.CreateProperty(organizationId, property);
            return createdProperty is null
                ? NotFound()
                : CreatedAtAction(nameof(GetProperty), new { id = createdProperty.Id }, createdProperty);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<ActionResult<PropertyModel>> UpdateProperty(int id, PropertyModel property)
        {
            if (id != property.Id) return BadRequest();
            if (await _service.GetPropertyById(id) is null) return NotFound();

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.CanManageProperty(id, identityUserId)) return Forbid();

            var updatedProperty = await _service.UpdateProperty(property);
            return updatedProperty is null ? NotFound() : Ok(updatedProperty);
        }

        [HttpPost("{id:int}/invite-ids/{inviteType}/rotate")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> RotateInviteId(int id, PropertyInviteType inviteType)
        {
            if (await _service.GetPropertyById(id) is null) return NotFound();

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.CanManageProperty(id, identityUserId)) return Forbid();

            var inviteId = await _service.RotateInviteId(id, inviteType);
            return inviteId is null ? NotFound() : Ok(new { inviteType, inviteId });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            if (await _service.GetPropertyById(id) is null) return NotFound();

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.CanManageProperty(id, identityUserId)) return Forbid();

            return await _service.DeleteProperty(id) switch
            {
                PropertyDeleteResult.Deleted => NoContent(),
                PropertyDeleteResult.NotFound => NotFound(),
                PropertyDeleteResult.HasEmployees => Conflict(),
                PropertyDeleteResult.HasReservations => Conflict(),
                _ => throw new InvalidOperationException("Unknown property deletion result.")
            };
        }
    }
}