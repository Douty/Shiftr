
using System.Text.Json.Serialization;

namespace Shiftr.Models
{
    public enum EmployeeType
    {
        Owner,
        Manager,
        FrontDesk
    }
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$employeeType")]
    [JsonDerivedType(typeof(OwnerModel), "owner")]
    [JsonDerivedType(typeof(ManagerModel), "manager")]
    [JsonDerivedType(typeof(FrontDeskAgentModel), "frontDesk")]
    public abstract class EmployeeBase
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public string? IdentityUserId { get; set; }
        public DateTime HireDate { get; set; }
        public abstract EmployeeType Type {get; }
        
        
    }
}