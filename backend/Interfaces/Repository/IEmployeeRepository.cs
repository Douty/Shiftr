
using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IEmployeeRepository
    {
        Task<EmployeeBase?> GetByIdAsync(int Id);
        Task<List<EmployeeBase>> GetAllAsync();
        Task<EmployeeBase> AddAsync(EmployeeBase employee);
        Task<EmployeeBase?> UpdateAsync(EmployeeBase employee);
        Task<bool> DeleteAsync(int Id);
        Task<bool> IsLinkedToIdentityAsync(int employeeId, string identityUserId);
        
    }
}