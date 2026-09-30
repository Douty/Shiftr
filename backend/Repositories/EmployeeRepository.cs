
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Shiftr.Data;
using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Repository
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ShiftrDbContext _context;
        public EmployeeRepository(ShiftrDbContext context)
        {
            _context = context;
        } 
        public async Task<EmployeeBase> AddAsync(EmployeeBase employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return employee;
        }

        public async Task<bool> DeleteAsync(int Id)
        {
            var employee = await GetByIdAsync(Id);
            
            if (employee is  null) return false;
            
            _context.Employees.Remove(employee);
            int rowAffected = await _context.SaveChangesAsync();
            return rowAffected > 0;
            
        }


        public async Task<List<EmployeeBase>> GetAllAsync()
        {
            return await _context.Employees.ToListAsync();
        }

        public async Task<EmployeeBase?> GetByIdAsync(int Id)
        {
            return await _context.Employees.FirstOrDefaultAsync(e => e.Id == Id);
        }

        public Task<bool> IsLinkedToIdentityAsync(int employeeId, string identityUserId) =>
            _context.Employees.AnyAsync(employee =>
                employee.Id == employeeId && employee.IdentityUserId == identityUserId);

       
        public async Task<EmployeeBase?> UpdateAsync(EmployeeBase employee)
        {
            var existingEmployee = await _context.Employees.FirstOrDefaultAsync(
                existing => existing.Id == employee.Id);
            if (existingEmployee is null || existingEmployee.GetType() != employee.GetType())
            {
                return null;
            }

            existingEmployee.FirstName = employee.FirstName;
            existingEmployee.LastName = employee.LastName;
            existingEmployee.Email = employee.Email;
            existingEmployee.PhoneNumber = employee.PhoneNumber;
            existingEmployee.HireDate = employee.HireDate;
            await _context.SaveChangesAsync();
            return existingEmployee;
        }
    }
}