
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Repository;

namespace Shiftr.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly EmployeeRepository _Repository;
        public EmployeeService(EmployeeRepository employeeRepository)
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

        public async Task<bool> DeleteEmployee(EmployeeBase employee)
        {
            return await _Repository.DeleteAsync(employee);
        }

        public async Task<EmployeeBase> UpdateEmployee(EmployeeBase Employee)
        {
            return await _Repository.UpdateAsync(Employee);
        }
        public async Task<EmployeeBase?> GetEmployeeByID(int EmployeeId)
        {
             throw new NotImplementedException();
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