using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Security;

namespace Shiftr.Controllers
{
    [ApiController]
    [Route("api/properties/{propertyId:int}/residents")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class ResidentsController : ControllerBase
    {
        private readonly IResidentService _residentService;
        private readonly IPropertyService _propertyService;

        public ResidentsController(IResidentService residentService, IPropertyService propertyService)
        {
            _residentService = residentService;
            _propertyService = propertyService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ResidentModel>>> GetResidents(int propertyId, CancellationToken cancellationToken)
        {
            var access = await CheckAdminAccess(propertyId);
            if (access is not null) return access;

            return Ok(await _residentService.GetResidents(propertyId, cancellationToken));
        }

        [HttpGet("{residentId:int}")]
        public async Task<ActionResult<ResidentModel>> GetResident(int propertyId, int residentId, CancellationToken cancellationToken)
        {
            var access = await CheckAdminAccess(propertyId);
            if (access is not null) return access;

            var resident = await _residentService.GetResident(propertyId, residentId, cancellationToken);
            return resident is null ? NotFound() : Ok(resident);
        }

        [HttpPost]
        public async Task<ActionResult<ResidentModel>> CreateResident(int propertyId, ResidentModel resident, CancellationToken cancellationToken)
        {
            var access = await CheckAdminAccess(propertyId);
            if (access is not null) return access;

            var createdResident = await _residentService.CreateResident(propertyId, resident, cancellationToken);
            return createdResident is null
                ? NotFound()
                : CreatedAtAction(nameof(GetResident), new { propertyId, residentId = createdResident.Id }, createdResident);
        }

        [HttpPut("{residentId:int}")]
        public async Task<ActionResult<ResidentModel>> UpdateResident(int propertyId, int residentId, ResidentModel resident, CancellationToken cancellationToken)
        {
            if (residentId != resident.Id) return BadRequest();
            var access = await CheckAdminAccess(propertyId);
            if (access is not null) return access;

            var updatedResident = await _residentService.UpdateResident(propertyId, resident, cancellationToken);
            return updatedResident is null ? NotFound() : Ok(updatedResident);
        }

        [HttpPost("{residentId:int}/allowed-guests")]
        public async Task<ActionResult<ResidentModel>> AddAllowedGuest(int propertyId, int residentId, [FromBody] string guestName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(guestName)) return BadRequest();
            var access = await CheckAdminAccess(propertyId);
            if (access is not null) return access;

            var resident = await _residentService.AddAllowedGuest(propertyId, residentId, guestName, cancellationToken);
            return resident is null ? NotFound() : Ok(resident);
        }

        [HttpDelete("{residentId:int}/allowed-guests")]
        public async Task<ActionResult<ResidentModel>> RemoveAllowedGuest(int propertyId, int residentId, [FromQuery] string guestName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(guestName)) return BadRequest();
            var access = await CheckAdminAccess(propertyId);
            if (access is not null) return access;

            var resident = await _residentService.RemoveAllowedGuest(propertyId, residentId, guestName, cancellationToken);
            return resident is null ? NotFound() : Ok(resident);
        }

        [HttpPut("{residentId:int}/call-to-notify")]
        public async Task<ActionResult<ResidentModel>> SetCallToNotify(int propertyId, int residentId, [FromBody] bool callToNotify, CancellationToken cancellationToken)
        {
            var access = await CheckAdminAccess(propertyId);
            if (access is not null) return access;

            var resident = await _residentService.SetCallToNotify(propertyId, residentId, callToNotify, cancellationToken);
            return resident is null ? NotFound() : Ok(resident);
        }

        [HttpDelete("{residentId:int}")]
        public async Task<IActionResult> DeleteResident(int propertyId, int residentId, CancellationToken cancellationToken)
        {
            var access = await CheckAdminAccess(propertyId);
            if (access is not null) return access;

            return await _residentService.DeleteResident(propertyId, residentId, cancellationToken)
                ? NoContent()
                : NotFound();
        }

        private async Task<ActionResult?> CheckAdminAccess(int propertyId)
        {
            if (await _propertyService.GetPropertyById(propertyId) is null) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return identityUserId is null || !await _propertyService.CanManageProperty(propertyId, identityUserId)
                ? Forbid()
                : null;
        }
    }
}