using Shiftr.Interfaces;

namespace Shiftr.Models
{
    public class ManagerModel : EmployeeBase
    {
        
        public EmployeeType Type => EmployeeType.Manager;

        
    }
}