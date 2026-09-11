
using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Repository
{
    public class EmployeeRepository : IEmployeeRepository
    {
        public Task<EmployeeBase> AddAsync(EmployeeBase employee)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(EmployeeBase employee)
        {
            throw new NotImplementedException();
        }

        public Task<List<EmployeeBase>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<EmployeeBase?> GetByIDAsync(int Id)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(EmployeeBase employee)
        {
            throw new NotImplementedException();
        }
    }
}