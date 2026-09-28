using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shiftr.Models;

namespace Shiftr.Data
{
    public class ShiftrDbContext : DbContext
    {
        public ShiftrDbContext(DbContextOptions<ShiftrDbContext> options) : base(options) {}

        public DbSet<EmployeeBase> Employees => Set<EmployeeBase>();
        public DbSet<PropertyModel> Properties => Set<PropertyModel>();
        public DbSet<OrganizationModel> Organizations => Set<OrganizationModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var shiftListConverter = new ValueConverter<List<Shift>, string>(
                shifts => JsonSerializer.Serialize(shifts),
                json => JsonSerializer.Deserialize<List<Shift>>(json) ?? new List<Shift>());
            var shiftListComparer = new ValueComparer<List<Shift>>(
                (left, right) => left != null && right != null && left.SequenceEqual(right),
                shifts => shifts.Aggregate(0, (hash, shift) => HashCode.Combine(hash, shift)),
                shifts => shifts.ToList());

            modelBuilder.Entity<FrontDeskAgentModel>()
                .Property(employee => employee.ShiftWorked)
                .HasConversion(shiftListConverter)
                .Metadata.SetValueComparer(shiftListComparer);
        }
    }
}