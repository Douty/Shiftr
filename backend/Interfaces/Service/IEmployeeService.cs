using Shiftr.Models;

namespace Shiftr.Interface 
{
    public interface IEmployeeService
    {
        Task<EmployeeBase?> GetEmployeeById(int Id);
        Task<EmployeeBase> CreateEmployee(EmployeeBase Employee);
        Task<EmployeeBase?> UpdateEmployee(EmployeeBase Employee);
        Task<bool> DeleteEmployee(int Id);
        Task<bool> CanAccessEmployee(int employeeId, string identityUserId);
        Task<bool> HasEmployeeProfile(string identityUserId);
        Task<bool> CanCreateEmployee(EmployeeBase employee, string identityUserId);
        bool IsAdmin(EmployeeBase Employee);
        bool IsOwner(EmployeeBase Employee);
    }
}