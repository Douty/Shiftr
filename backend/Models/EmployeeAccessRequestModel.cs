using System.Text.Json.Serialization;

namespace Shiftr.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmployeeAccessRole
{
    Manager,
    FrontDesk
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmployeeAccessRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public class EmployeeAccessRequestModel
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public required string IdentityUserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public EmployeeAccessRole Role { get; set; }
    public EmployeeAccessRequestStatus Status { get; set; } = EmployeeAccessRequestStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public PropertyModel? Property { get; set; }
}