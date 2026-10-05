using Shiftr.Models;

namespace Shiftr.DTOs;

public record EmployeeAccessRequestResponse(
    int Id,
    int PropertyId,
    string PropertyName,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    EmployeeAccessRole Role,
    EmployeeAccessRequestStatus Status,
    DateTimeOffset CreatedAt);