
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
        public void CreateEmployee(EmployeeBase Employee)
        {
            throw new NotImplementedException();
        }

        public bool DeleteEmployee(EmployeeBase Employee)
        {
            throw new NotImplementedException();
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

        public void UpdateEmployee(EmployeeBase Employee)
        {
            throw new NotImplementedException();
        }
    }
}