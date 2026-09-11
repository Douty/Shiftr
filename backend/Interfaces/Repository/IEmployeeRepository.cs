
using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IEmployeeRepository
    {
        Task<EmployeeBase?> GetByIDAsync(int Id);
        Task<List<EmployeeBase>> GetAllAsync();
        Task<EmployeeBase> AddAsync(EmployeeBase employee);
        Task UpdateAsync(EmployeeBase employee);
        Task DeleteAsync(EmployeeBase employee);
        
    }
}