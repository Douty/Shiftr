namespace Shiftr.DTOs
{
    /// <summary>Resident amenity reservation details.</summary>
    public sealed record AmenityReservationResponse(
        int Id,
        int AmenityTypeId,
        string ResidentIdentityUserId,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        string? Notes);
}