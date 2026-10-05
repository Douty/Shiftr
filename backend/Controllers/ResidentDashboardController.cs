using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shiftr.DTOs;
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Security;

namespace Shiftr.Controllers;

[ApiController]
[Route("api/resident")]
[Authorize(Policy = AuthorizationPolicies.ResidentOnly)]
public sealed class ResidentDashboardController : ControllerBase
{
    private readonly IResidentService _residentService;
    private readonly IAmenityService _amenityService;

    public ResidentDashboardController(IResidentService residentService, IAmenityService amenityService)
    {
        _residentService = residentService;
        _amenityService = amenityService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ResidentDashboardProfileResponse>> GetProfile(CancellationToken cancellationToken)
    {
        var resident = await GetCurrentResident(cancellationToken);
        return resident is null || resident.Property is null
            ? NotFound()
            : Ok(ToProfileResponse(resident));
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<ResidentDashboardProfileResponse>> UpdatePreferences(
        UpdateResidentPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();
        if (request.AllowedGuests is null ||
            request.AllowedGuests.Any(guest => string.IsNullOrWhiteSpace(guest) || guest.Trim().Length > 100))
        {
            return BadRequest("Guest names must contain between 1 and 100 characters.");
        }

        var allowedGuests = request.AllowedGuests
            .Select(guest => guest.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var resident = await _residentService.UpdatePreferences(
            identityUserId, request.CallToNotify, allowedGuests, cancellationToken);
        return resident is null || resident.Property is null
            ? NotFound()
            : Ok(ToProfileResponse(resident));
    }

    [HttpGet("amenities")]
    public async Task<ActionResult<IReadOnlyList<AmenityTypeResponse>>> GetAmenities(CancellationToken cancellationToken)
    {
        var resident = await GetCurrentResident(cancellationToken);
        if (resident is null) return NotFound();

        var amenities = await _amenityService.GetAmenities(resident.PropertyId, cancellationToken);
        return Ok(amenities.Select(amenity => new AmenityTypeResponse(
            amenity.Id, amenity.PropertyId, amenity.Name, amenity.Description)).ToList());
    }

    [HttpGet("reservations")]
    public async Task<ActionResult<IReadOnlyList<AmenityReservationResponse>>> GetReservations(CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();
        var resident = await _residentService.GetResidentByIdentityUserId(identityUserId, cancellationToken);
        if (resident is null) return NotFound();

        var reservations = await _amenityService.GetReservationsForResident(
            resident.PropertyId, identityUserId, cancellationToken);
        return Ok(reservations.Select(reservation => ToReservationResponse(reservation, resident.Id)).ToList());
    }

    [HttpPost("reservations")]
    public async Task<IActionResult> CreateReservation(
        CreateAmenityReservationRequest request,
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();
        var resident = await _residentService.GetResidentByIdentityUserId(identityUserId, cancellationToken);
        if (resident is null) return NotFound();

        var (result, reservation) = await _amenityService.CreateReservation(
            resident.PropertyId,
            request.AmenityTypeId,
            identityUserId,
            request.StartsAt,
            request.EndsAt,
            request.Notes,
            cancellationToken);
        if (result != AmenityReservationResult.Success) return MapFailure(result);

        var response = ToReservationResponse(reservation!, resident.Id);
        return Created($"/api/resident/reservations/{response.Id}", response);
    }

    [HttpPut("reservations/{reservationId:int}")]
    public async Task<IActionResult> UpdateReservation(
        int reservationId,
        UpdateAmenityReservationRequest request,
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();
        var resident = await _residentService.GetResidentByIdentityUserId(identityUserId, cancellationToken);
        if (resident is null) return NotFound();

        var (result, reservation) = await _amenityService.UpdateResidentReservation(
            resident.PropertyId,
            reservationId,
            request.AmenityTypeId,
            identityUserId,
            request.StartsAt,
            request.EndsAt,
            request.Notes,
            cancellationToken);
        if (result != AmenityReservationResult.Success) return MapFailure(result);
        return Ok(ToReservationResponse(reservation!, resident.Id));
    }

    [HttpDelete("reservations/{reservationId:int}")]
    public async Task<IActionResult> DeleteReservation(int reservationId, CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identityUserId is null) return Unauthorized();
        var resident = await _residentService.GetResidentByIdentityUserId(identityUserId, cancellationToken);
        if (resident is null) return NotFound();

        return await _amenityService.DeleteResidentReservation(
            resident.PropertyId, reservationId, identityUserId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private Task<ResidentModel?> GetCurrentResident(CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return identityUserId is null
            ? Task.FromResult<ResidentModel?>(null)
            : _residentService.GetResidentByIdentityUserId(identityUserId, cancellationToken);
    }

    private static ResidentDashboardProfileResponse ToProfileResponse(ResidentModel resident) =>
        new(
            resident.Id,
            resident.PropertyId,
            resident.Property!.Name,
            resident.FirstName,
            resident.LastName,
            resident.UnitNumber,
            resident.CallToNotify,
            resident.AllowedGuests);

    private static AmenityReservationResponse ToReservationResponse(AmenityReservationModel reservation, int residentId) =>
        new(
            reservation.Id,
            reservation.AmenityTypeId,
            reservation.ResidentIdentityUserId,
            residentId,
            reservation.StartsAt,
            reservation.EndsAt,
            reservation.Notes);

    private static IActionResult MapFailure(AmenityReservationResult result) => result switch
    {
        AmenityReservationResult.NotFound => new NotFoundResult(),
        AmenityReservationResult.AmenityNotFound => new NotFoundObjectResult("That amenity is no longer available."),
        AmenityReservationResult.ResidentNotFound => new NotFoundObjectResult("Resident profile not found."),
        AmenityReservationResult.InvalidTimeRange => new BadRequestObjectResult("End time must be after start time."),
        AmenityReservationResult.TimeConflict => new ConflictObjectResult("That amenity is already reserved during this time."),
        _ => throw new InvalidOperationException("Unknown amenity reservation result.")
    };
}
