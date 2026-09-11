using Shiftr.Models;

namespace Shiftr.Interface 
{
    public interface IEmployeeService
    {
        Task<EmployeeBase?> GetEmployeeByID(int Id);
        void CreateEmployee(EmployeeBase Employee);
        void UpdateEmployee(EmployeeBase Employee);
        bool DeleteEmployee(EmployeeBase Employee);
        bool IsAdmin(EmployeeBase Employee);
        bool IsOwner(EmployeeBase Employee);
    }
}