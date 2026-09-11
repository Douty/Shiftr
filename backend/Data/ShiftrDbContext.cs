using Microsoft.EntityFrameworkCore;
using Shiftr.Models;

namespace Shiftr.Data
{
    public class ShiftrDbContext : DbContext
    {
        public ShiftrDbContext(DbContextOptions<ShiftrDbContext> options) : base(options) {}

        public DbSet<EmployeeBase> Employees => Set<EmployeeBase>();
        public DbSet<PropertyModel> Properties => Set<PropertyModel>();
        public DbSet<OrganizationModel> Organizations => Set<OrganizationModel>();
        
    }
}