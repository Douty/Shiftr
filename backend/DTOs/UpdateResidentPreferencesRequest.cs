using System.ComponentModel.DataAnnotations;

namespace Shiftr.DTOs;

public sealed class UpdateResidentPreferencesRequest
{
    public bool CallToNotify { get; init; }

    [MaxLength(50)]
    public List<string> AllowedGuests { get; init; } = [];
}
