
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Repository;

namespace Shiftr.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _Repository;
        public EmployeeService(IEmployeeRepository employeeRepository)
        {
            _Repository = employeeRepository;
        }
        public async Task<EmployeeBase?> GetEmployeeById(int Id)
        {
            return await _Repository.GetByIdAsync(Id);
        }
        
        public async Task<EmployeeBase> CreateEmployee(EmployeeBase Employee)
        {
            return await _Repository.AddAsync(Employee);
        }

        public async Task<bool> DeleteEmployee(int Id)
        {
            return await _Repository.DeleteAsync(Id);
        }

        public async Task<EmployeeBase> UpdateEmployee(EmployeeBase Employee)
        {
            return await _Repository.UpdateAsync(Employee);
        }
      
        public bool IsAdmin(EmployeeBase Employee)
        {
            return Employee.Type == EmployeeType.Manager || Employee.Type == EmployeeType.Owner;
        }
        public bool IsOwner(EmployeeBase Employee)
        {
            return Employee.Type == EmployeeType.Owner;
        }
      
    }
}