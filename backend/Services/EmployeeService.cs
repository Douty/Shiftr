
using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Services
{
    public class EmployeeService : IEmployeeService
    {
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