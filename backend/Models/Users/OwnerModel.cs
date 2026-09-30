

using System.Text.Json.Serialization;

namespace Shiftr.Models
{
    public class OwnerModel : EmployeeBase
    {
        public override EmployeeType Type => EmployeeType.Owner;
        public required int OrganizationID {get; set;}
        [JsonIgnore]
        public OrganizationModel? Organization {get; set;}
    }
}