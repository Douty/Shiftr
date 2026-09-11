using Shiftr.Models;

namespace Shiftr.Interface 
{
    public interface IEmployeeService
    {
        Task<EmployeeBase?> GetEmployeeByID(int Id);
        Task<EmployeeBase> CreateEmployee(EmployeeBase Employee);
        Task<EmployeeBase> UpdateEmployee(EmployeeBase Employee);
        Task<bool> DeleteEmployee(EmployeeBase Employee);
        bool IsAdmin(EmployeeBase Employee);
        bool IsOwner(EmployeeBase Employee);
    }
}