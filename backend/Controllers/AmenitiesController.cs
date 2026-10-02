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
    [Route("api/properties/{propertyId:int}/amenities")]
    public class AmenitiesController : ControllerBase
    {
        private readonly IAmenityService _amenityService;
        private readonly IPropertyService _propertyService;

        public AmenitiesController(IAmenityService amenityService, IPropertyService propertyService)
        {
            _amenityService = amenityService;
            _propertyService = propertyService;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.RegularEmployee)]
        public async Task<ActionResult<IReadOnlyList<AmenityTypeResponse>>> GetAmenities(int propertyId, CancellationToken cancellationToken)
        {
            var property = await _propertyService.GetPropertyById(propertyId);
            if (property is null) return NotFound();
            if (!await CanAccessProperty(propertyId, cancellationToken)) return Forbid();

            var amenities = await _amenityService.GetAmenities(propertyId, cancellationToken);
            return Ok(amenities.Select(ToResponse).ToList());
        }

        [HttpGet("{amenityId:int}")]
        [Authorize(Policy = AuthorizationPolicies.RegularEmployee)]
        public async Task<ActionResult<AmenityTypeResponse>> GetAmenity(int propertyId, int amenityId, CancellationToken cancellationToken)
        {
            var property = await _propertyService.GetPropertyById(propertyId);
            if (property is null) return NotFound();
            if (!await CanAccessProperty(propertyId, cancellationToken)) return Forbid();

            var amenity = await _amenityService.GetAmenity(propertyId, amenityId, cancellationToken);
            return amenity is null ? NotFound() : Ok(ToResponse(amenity));
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> CreateAmenity(int propertyId, CreateAmenityTypeRequest request, CancellationToken cancellationToken)
        {
            var access = await CheckAdminAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            var (amenity, nameConflict) = await _amenityService.CreateAmenity(propertyId, request.Name, request.Description, cancellationToken);
            if (nameConflict) return Conflict("An amenity with this name already exists for the property.");
            if (amenity is null) return NotFound();

            var response = ToResponse(amenity);
            return CreatedAtAction(nameof(GetAmenity), new { propertyId, amenityId = amenity.Id }, response);
        }

        [HttpPut("{amenityId:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateAmenity(int propertyId, int amenityId, UpdateAmenityTypeRequest request, CancellationToken cancellationToken)
        {
            var access = await CheckAdminAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            var (amenity, nameConflict) = await _amenityService.UpdateAmenity(propertyId, amenityId, request.Name, request.Description, cancellationToken);
            if (nameConflict) return Conflict("An amenity with this name already exists for the property.");
            return amenity is null ? NotFound() : Ok(ToResponse(amenity));
        }

        [HttpDelete("{amenityId:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> DeleteAmenity(int propertyId, int amenityId, CancellationToken cancellationToken)
        {
            var access = await CheckAdminAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            return await _amenityService.DeleteAmenity(propertyId, amenityId, cancellationToken) switch
            {
                AmenityDeleteResult.Deleted => NoContent(),
                AmenityDeleteResult.NotFound => NotFound(),
                AmenityDeleteResult.HasReservations => Conflict(),
                _ => throw new InvalidOperationException("Unknown amenity deletion result.")
            };
        }

        private async Task<bool> CanAccessProperty(int propertyId, CancellationToken cancellationToken)
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return identityUserId is not null && await _propertyService.CanAccessProperty(propertyId, identityUserId);
        }

        private async Task<IActionResult?> CheckAdminAccess(int propertyId, CancellationToken cancellationToken)
        {
            if (!await _amenityService.PropertyExists(propertyId, cancellationToken)) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return identityUserId is null || !await _propertyService.CanManageProperty(propertyId, identityUserId)
                ? Forbid()
                : null;
        }

        private static AmenityTypeResponse ToResponse(AmenityTypeModel amenity) =>
            new(amenity.Id, amenity.PropertyId, amenity.Name, amenity.Description);
    }
}