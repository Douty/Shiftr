using Shiftr.Models;

namespace Shiftr.Interface 
{
    public interface IEmployeeService
    {
        Task<EmployeeBase?> GetEmployeeByID(int Id);
        bool IsAdmin(EmployeeBase Employee);
    }
}