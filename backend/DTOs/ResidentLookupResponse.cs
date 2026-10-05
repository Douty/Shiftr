namespace Shiftr.DTOs;

public sealed record ResidentLookupResponse(
    int Id,
    string FirstName,
    string LastName,
    string? UnitNumber,
    bool CanBookAmenities);
