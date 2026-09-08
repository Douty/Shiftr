

namespace Shiftr.Models
{
    public class OwnerModel : EmployeeBase
    {
        public override EmployeeType Type => EmployeeType.Owner;
    }
}