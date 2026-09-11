

namespace Shiftr.Models
{
    public class OwnerModel : EmployeeBase
    {
        public override EmployeeType Type => EmployeeType.Owner;
        public required int OrganizationID {get; set;}
        public OrganizationModel? Organization {get; set;}
    }
}