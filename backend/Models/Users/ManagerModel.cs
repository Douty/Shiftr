using Shiftr.Interfaces;

namespace Shiftr.Models
{
    public class ManagerModel : EmployeeBase
    {
        
        public  override EmployeeType Type => EmployeeType.Manager;

        
    }
}