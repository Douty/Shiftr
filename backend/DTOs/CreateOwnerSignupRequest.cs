using System.ComponentModel.DataAnnotations;

namespace Shiftr.DTOs;

public class CreateOwnerSignupRequest
{
    [Required, StringLength(200)]
    public required string OrganizationName { get; set; }

    [Required, StringLength(100)]
    public required string FirstName { get; set; }

    [Required, StringLength(100)]
    public required string LastName { get; set; }

    [Required, Phone]
    public required string PhoneNumber { get; set; }
}
