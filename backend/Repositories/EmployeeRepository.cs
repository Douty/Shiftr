
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

        public async Task DeleteAsync(int Id)
        {
            EmployeeBase? Employee = await GetByIdAsync(Id);
            if (Employee is not null)
            {
                _context.Employees.Remove(Employee);
                await _context.SaveChangesAsync();
            }
        }

        public Task DeleteAsync(EmployeeBase employee)
        {
            throw new NotImplementedException();
        }

        public async Task<List<EmployeeBase>> GetAllAsync()
        {
            return await _context.Employees.ToListAsync();
        }

        public async Task<EmployeeBase?> GetByIdAsync(int Id)
        {
            return await _context.Employees.FirstOrDefaultAsync(e => e.Id == Id);
        }

       
        public async Task UpdateAsync(EmployeeBase employee)
        {
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }
    }
}