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
    [Authorize(Roles = IdentityRoles.FrontDesk)]
    public class AmenityReservationsController : ControllerBase
    {
        private readonly IAmenityService _amenityService;
        private readonly IPropertyService _propertyService;

        public AmenityReservationsController(IAmenityService amenityService, IPropertyService propertyService)
        {
            _amenityService = amenityService;
            _propertyService = propertyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetReservations(int propertyId, CancellationToken cancellationToken)
        {
            var access = await CheckPropertyAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            var reservations = await _amenityService.GetReservations(propertyId, cancellationToken);
            return Ok(reservations.Select(ToResponse).ToList());
        }

        [HttpGet("{reservationId:int}")]
        public async Task<IActionResult> GetReservation(int propertyId, int reservationId, CancellationToken cancellationToken)
        {
            var access = await CheckPropertyAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            var reservation = await _amenityService.GetReservation(propertyId, reservationId, cancellationToken);
            return reservation is null ? NotFound() : Ok(ToResponse(reservation));
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation(int propertyId, CreateAmenityReservationRequest request, CancellationToken cancellationToken)
        {
            var access = await CheckPropertyAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            var (result, reservation) = await _amenityService.CreateReservation(
                propertyId, request.AmenityTypeId, request.ResidentIdentityUserId,
                request.StartsAt, request.EndsAt, request.Notes, cancellationToken);
            if (result != AmenityReservationResult.Success) return MapFailure(result);

            var response = ToResponse(reservation!);
            return CreatedAtAction(nameof(GetReservation), new { propertyId, reservationId = response.Id }, response);
        }

        [HttpPut("{reservationId:int}")]
        public async Task<IActionResult> UpdateReservation(int propertyId, int reservationId, UpdateAmenityReservationRequest request, CancellationToken cancellationToken)
        {
            var access = await CheckPropertyAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            var (result, reservation) = await _amenityService.UpdateReservation(
                propertyId, reservationId, request.AmenityTypeId, request.ResidentIdentityUserId,
                request.StartsAt, request.EndsAt, request.Notes, cancellationToken);
            if (result != AmenityReservationResult.Success) return MapFailure(result);
            return Ok(ToResponse(reservation!));
        }

        [HttpDelete("{reservationId:int}")]
        public async Task<IActionResult> DeleteReservation(int propertyId, int reservationId, CancellationToken cancellationToken)
        {
            var access = await CheckPropertyAccess(propertyId, cancellationToken);
            if (access is not null) return access;

            return await _amenityService.DeleteReservation(propertyId, reservationId, cancellationToken)
                ? NoContent()
                : NotFound();
        }

        private async Task<IActionResult?> CheckPropertyAccess(int propertyId, CancellationToken cancellationToken)
        {
            if (!await _amenityService.PropertyExists(propertyId, cancellationToken)) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return identityUserId is null || !await _propertyService.CanManageReservations(propertyId, identityUserId, cancellationToken)
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

        private static AmenityReservationResponse ToResponse(AmenityReservationModel reservation) =>
            new(reservation.Id, reservation.AmenityTypeId, reservation.ResidentIdentityUserId,
                reservation.StartsAt, reservation.EndsAt, reservation.Notes);
    }
}