using System.ComponentModel.DataAnnotations;

namespace Shiftr.DTOs;

public sealed class CreateResidentSignupRequest
{
    [Required, StringLength(100)]
    public required string InviteCode { get; init; }

    [Required, StringLength(100)]
    public required string FirstName { get; init; }

    [Required, StringLength(100)]
    public required string LastName { get; init; }

    [StringLength(30)]
    public string? UnitNumber { get; init; }
}
