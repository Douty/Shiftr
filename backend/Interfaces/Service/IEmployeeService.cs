using Shiftr.Models;

namespace Shiftr.Interface 
{
    public interface IEmployeeService
    {
        Task<EmployeeBase?> GetEmployeeByID(int Id);
        void CreateEmployee();
        bool IsAdmin(EmployeeBase Employee);
        bool IsOwner(EmployeeBase Employee);
    }
}