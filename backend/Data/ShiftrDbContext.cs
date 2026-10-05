using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.Identity;
using Shiftr.Models;

namespace Shiftr.Data
{
    public class ShiftrDbContext : IdentityDbContext<IdentityUser>
    {
        public ShiftrDbContext(DbContextOptions<ShiftrDbContext> options) : base(options) {}

        public DbSet<EmployeeBase> Employees => Set<EmployeeBase>();
        public DbSet<ResidentModel> Residents => Set<ResidentModel>();
        public DbSet<PropertyModel> Properties => Set<PropertyModel>();
        public DbSet<OrganizationModel> Organizations => Set<OrganizationModel>();
        public DbSet<AmenityTypeModel> Amenities => Set<AmenityTypeModel>();
        public DbSet<AmenityReservationModel> AmenityReservations => Set<AmenityReservationModel>();
        public DbSet<ShiftNoteModel> ShiftNotes => Set<ShiftNoteModel>();
        public DbSet<EmployeeAccessRequestModel> EmployeeAccessRequests => Set<EmployeeAccessRequestModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EmployeeBase>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(employee => employee.IdentityUserId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<EmployeeBase>()
                .HasIndex(employee => employee.IdentityUserId)
                .IsUnique();

            modelBuilder.Entity<PropertyModel>()
                .HasIndex(property => property.ResidentInviteId)
                .IsUnique();
            modelBuilder.Entity<PropertyModel>()
                .HasIndex(property => property.EmployeeInviteId)
                .IsUnique();

            modelBuilder.Entity<ResidentModel>()
                .HasOne(resident => resident.Property)
                .WithMany(property => property.Residents)
                .HasForeignKey(resident => resident.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ResidentModel>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(resident => resident.IdentityUserId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<ResidentModel>()
                .HasIndex(resident => resident.IdentityUserId)
                .IsUnique();

            var allowedGuestsConverter = new ValueConverter<List<string>, string>(
                guests => JsonSerializer.Serialize(guests),
                json => JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>());
            var allowedGuestsComparer = new ValueComparer<List<string>>(
                (left, right) => left != null && right != null && left.SequenceEqual(right),
                guests => guests.Aggregate(0, (hash, guest) => HashCode.Combine(hash, guest)),
                guests => guests.ToList());

            modelBuilder.Entity<ResidentModel>()
                .Property(resident => resident.AllowedGuests)
                .HasConversion(allowedGuestsConverter)
                .Metadata.SetValueComparer(allowedGuestsComparer);

            modelBuilder.Entity<AmenityTypeModel>()
                .HasOne(amenity => amenity.Property)
                .WithMany(property => property.Amenities)
                .HasForeignKey(amenity => amenity.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<AmenityTypeModel>()
                .HasIndex(amenity => new { amenity.PropertyId, amenity.Name })
                .IsUnique();

            modelBuilder.Entity<AmenityReservationModel>()
                .HasOne(reservation => reservation.Amenity)
                .WithMany(amenity => amenity.Reservations)
                .HasForeignKey(reservation => reservation.AmenityTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AmenityReservationModel>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(reservation => reservation.ResidentIdentityUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftNoteModel>()
                .HasOne<PropertyModel>()
                .WithMany()
                .HasForeignKey(note => note.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ShiftNoteModel>()
                .HasOne(note => note.AuthorEmployee)
                .WithMany()
                .HasForeignKey(note => note.AuthorEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ShiftNoteModel>()
                .HasIndex(note => new { note.PropertyId, note.UpdatedAt });

            modelBuilder.Entity<EmployeeAccessRequestModel>()
                .HasOne(request => request.Property)
                .WithMany()
                .HasForeignKey(request => request.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EmployeeAccessRequestModel>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(request => request.IdentityUserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EmployeeAccessRequestModel>()
                .HasIndex(request => new { request.PropertyId, request.Status });

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