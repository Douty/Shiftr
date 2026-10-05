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
    [Route("api/properties/{propertyId:int}/reservations")]
    [Authorize(Policy = AuthorizationPolicies.RegularEmployee)]
    public class AmenityReservationsController : ControllerBase
    {
        private readonly IAmenityService _amenityService;
        private readonly IPropertyService _propertyService;
        private readonly IResidentService _residentService;

        public AmenityReservationsController(
            IAmenityService amenityService,
            IPropertyService propertyService,
            IResidentService residentService)
        {
            _amenityService = amenityService;
            _propertyService = propertyService;
            _residentService = residentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetReservations(int propertyId, CancellationToken cancellationToken)
        {
            var access = await CheckPropertyReadAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            var reservations = await _amenityService.GetReservations(propertyId, cancellationToken);
            var residents = await _residentService.GetResidents(propertyId, cancellationToken);
            return Ok(reservations.Select(reservation => ToResponse(
                reservation,
                residents.FirstOrDefault(resident => resident.IdentityUserId == reservation.ResidentIdentityUserId)?.Id)).ToList());
        }

        [HttpGet("{reservationId:int}")]
        public async Task<IActionResult> GetReservation(int propertyId, int reservationId, CancellationToken cancellationToken)
        {
            var access = await CheckPropertyReadAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            var reservation = await _amenityService.GetReservation(propertyId, reservationId, cancellationToken);
            if (reservation is null) return NotFound();
            var resident = await _residentService.GetResidents(propertyId, cancellationToken);
            return Ok(ToResponse(
                reservation,
                resident.FirstOrDefault(item => item.IdentityUserId == reservation.ResidentIdentityUserId)?.Id));
        }

        [HttpPost]
        [Authorize(Roles = IdentityRoles.Owner + "," + IdentityRoles.FrontDesk + "," + IdentityRoles.Admin)]
        public async Task<IActionResult> CreateReservation(int propertyId, CreateAmenityReservationRequest request, CancellationToken cancellationToken)
        {
            var access = await CheckPropertyReadAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            var residentIdentityUserId = await ResolveResidentIdentityUserId(
                propertyId, request.ResidentId, request.ResidentIdentityUserId, cancellationToken);
            if (residentIdentityUserId is null) return BadRequest("Select a resident assigned to this property.");

            var (result, reservation) = await _amenityService.CreateReservation(
                propertyId, request.AmenityTypeId, residentIdentityUserId,
                request.StartsAt, request.EndsAt, request.Notes, cancellationToken);
            if (result != AmenityReservationResult.Success) return MapFailure(result);

            var response = ToResponse(reservation!, request.ResidentId);
            return CreatedAtAction(nameof(GetReservation), new { propertyId, reservationId = response.Id }, response);
        }

        [HttpPut("{reservationId:int}")]
        [Authorize(Roles = IdentityRoles.Owner + "," + IdentityRoles.FrontDesk + "," + IdentityRoles.Admin)]
        public async Task<IActionResult> UpdateReservation(int propertyId, int reservationId, UpdateAmenityReservationRequest request, CancellationToken cancellationToken)
        {
            var access = await CheckPropertyReadAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            var residentIdentityUserId = await ResolveResidentIdentityUserId(
                propertyId, request.ResidentId, request.ResidentIdentityUserId, cancellationToken);
            if (residentIdentityUserId is null) return BadRequest("Select a resident assigned to this property.");

            var (result, reservation) = await _amenityService.UpdateReservation(
                propertyId, reservationId, request.AmenityTypeId, residentIdentityUserId,
                request.StartsAt, request.EndsAt, request.Notes, cancellationToken);
            if (result != AmenityReservationResult.Success) return MapFailure(result);
            return Ok(ToResponse(reservation!, request.ResidentId));
        }

        [HttpDelete("{reservationId:int}")]
        [Authorize(Roles = IdentityRoles.Owner + "," + IdentityRoles.FrontDesk + "," + IdentityRoles.Admin)]
        public async Task<IActionResult> DeleteReservation(int propertyId, int reservationId, CancellationToken cancellationToken)
        {
            var access = await CheckPropertyReadAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            return await _amenityService.DeleteReservation(propertyId, reservationId, cancellationToken)
                ? NoContent()
                : NotFound();
        }

        private async Task<string?> ResolveResidentIdentityUserId(
            int propertyId,
            int? residentId,
            string? residentIdentityUserId,
            CancellationToken cancellationToken)
        {
            if (residentId.HasValue)
            {
                var resident = await _residentService.GetResident(propertyId, residentId.Value, cancellationToken);
                return resident?.IdentityUserId;
            }

            if (string.IsNullOrWhiteSpace(residentIdentityUserId)) return null;
            var residents = await _residentService.GetResidents(propertyId, cancellationToken);
            return residents.Any(resident => resident.IdentityUserId == residentIdentityUserId)
                ? residentIdentityUserId
                : null;
        }

        private async Task<IActionResult?> CheckPropertyReadAccess(int propertyId, CancellationToken cancellationToken)
        {
            if (!await _amenityService.PropertyExists(propertyId, cancellationToken)) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var canAccess = identityUserId is not null && (User.IsInRole(IdentityRoles.Owner)
                ? await _propertyService.CanAccessProperty(propertyId, identityUserId)
                : await _propertyService.CanAccessAssignedProperty(propertyId, identityUserId, cancellationToken));
            return !canAccess
                ? Forbid()
                : null;
        }

        private static IActionResult MapFailure(AmenityReservationResult result) => result switch
        {
            AmenityReservationResult.NotFound => new NotFoundResult(),
            AmenityReservationResult.AmenityNotFound => new NotFoundObjectResult("Amenity type does not exist for this property."),
            AmenityReservationResult.ResidentNotFound => new NotFoundObjectResult("Resident Identity user does not exist."),
            AmenityReservationResult.InvalidTimeRange => new BadRequestObjectResult("Reservation end time must be after its start time."),
            AmenityReservationResult.TimeConflict => new ConflictObjectResult("The amenity is already reserved during that time."),
            _ => throw new InvalidOperationException("Unknown reservation result.")
        };

        private static AmenityReservationResponse ToResponse(AmenityReservationModel reservation, int? residentId = null) =>
            new(reservation.Id, reservation.AmenityTypeId, reservation.ResidentIdentityUserId,
                residentId, reservation.StartsAt, reservation.EndsAt, reservation.Notes);
    }
}