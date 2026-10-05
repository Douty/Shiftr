using System.ComponentModel.DataAnnotations;
using Shiftr.Models;

namespace Shiftr.DTOs;

public class CreateEmployeeAccessRequestRequest
{
    [Required]
    public required string EmployeeInviteCode { get; set; }

    [Required, StringLength(100)]
    public required string FirstName { get; set; }

    [Required, StringLength(100)]
    public required string LastName { get; set; }

    [Required, Phone]
    public required string PhoneNumber { get; set; }

    [EnumDataType(typeof(EmployeeAccessRole))]
    public EmployeeAccessRole Role { get; set; }
}