namespace Shiftr.DTOs;

public sealed record ResidentDashboardProfileResponse(
    int Id,
    int PropertyId,
    string PropertyName,
    string FirstName,
    string LastName,
    string? UnitNumber,
    bool CallToNotify,
    IReadOnlyList<string> AllowedGuests);
