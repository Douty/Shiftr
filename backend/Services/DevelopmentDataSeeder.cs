using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shiftr.Data;
using Shiftr.Models;
using Shiftr.Security;

namespace Shiftr.Services;

public static class DevelopmentDataSeeder
{
    private const int ResidentTarget = 150;
    private const int EmployeeTarget = 30;
    private const int AmenityTargetPerProperty = 3;
    private const int ReservationTargetPerProperty = 12;
    private const int ShiftNoteTargetPerProperty = 8;

    private static readonly (string Name, string Description)[] SampleAmenities =
    [
        ("Community Room", "A flexible space for resident gatherings and events."),
        ("Fitness Center", "A shared fitness room with cardio and strength equipment."),
        ("Rooftop Terrace", "An outdoor space available for resident use.")
    ];

    public static async Task SeedAsync(
        ShiftrDbContext database,
        UserManager<IdentityUser> userManager,
        string password,
        CancellationToken cancellationToken = default)
    {
        var organizations = await database.Organizations
            .Include(organization => organization.Owners)
            .Include(organization => organization.Properties)
                .ThenInclude(property => property.Managers)
            .Include(organization => organization.Properties)
                .ThenInclude(property => property.FrontDeskAgents)
            .Include(organization => organization.Properties)
                .ThenInclude(property => property.Residents)
            .Include(organization => organization.Properties)
                .ThenInclude(property => property.Amenities)
                    .ThenInclude(amenity => amenity.Reservations)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        foreach (var organization in organizations)
        {
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            await SeedOrganizationAsync(database, userManager, organization, password, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private static async Task SeedOrganizationAsync(
        ShiftrDbContext database,
        UserManager<IdentityUser> userManager,
        OrganizationModel organization,
        string password,
        CancellationToken cancellationToken)
    {
        if (organization.Properties.Count == 0)
        {
            var property = new PropertyModel { Name = $"{organization.Name} Sample Property" };
            organization.Properties.Add(property);
            await database.SaveChangesAsync(cancellationToken);
        }

        var properties = organization.Properties;
        await EnsureEmployeesAsync(database, userManager, organization, password);
        await EnsureResidentsAsync(database, userManager, organization, password, cancellationToken);

        foreach (var property in properties)
        {
            await EnsureAmenitiesAsync(database, property, cancellationToken);
            await EnsureReservationsAsync(database, property, cancellationToken);
            await EnsureShiftNotesAsync(database, property, organization, cancellationToken);
            await EnsureAccessRequestsAsync(database, property, password, userManager, cancellationToken);
        }
    }

    private static async Task EnsureEmployeesAsync(
        ShiftrDbContext database,
        UserManager<IdentityUser> userManager,
        OrganizationModel organization,
        string password)
    {
        var properties = organization.Properties;
        var employeeCount = CountEmployees(organization);

        if (organization.Owners.Count == 0)
        {
            var identity = await CreateAccountAsync(
                userManager, $"sample.owner.org{organization.Id}@example.test", password, IdentityRoles.Owner);
            var owner = new OwnerModel
            {
                FirstName = "Sample",
                LastName = "Owner",
                Email = identity.Email!,
                PhoneNumber = "555-0100",
                HireDate = DateTime.UtcNow.AddYears(-5),
                IdentityUserId = identity.Id,
                OrganizationID = organization.Id
            };
            database.Employees.Add(owner);
            employeeCount++;
        }

        if (!properties.Any(property => property.Managers.Count > 0))
        {
            await AddEmployeeAsync(
                database, userManager, organization, EmployeeType.Manager, properties[0], password, employeeCount + 1);
            employeeCount++;
        }

        if (!properties.Any(property => property.FrontDeskAgents.Count > 0))
        {
            await AddEmployeeAsync(
                database, userManager, organization, EmployeeType.FrontDesk, properties[0], password, employeeCount + 1);
            employeeCount++;
        }

        while (employeeCount < EmployeeTarget)
        {
            var property = properties[employeeCount % properties.Count];
            await AddEmployeeAsync(
                database, userManager, organization, EmployeeType.FrontDesk, property, password, employeeCount + 1);
            employeeCount++;
        }
    }

    private static async Task AddEmployeeAsync(
        ShiftrDbContext database,
        UserManager<IdentityUser> userManager,
        OrganizationModel organization,
        EmployeeType type,
        PropertyModel property,
        string password,
        int sequence)
    {
        var role = type switch
        {
            EmployeeType.Owner => IdentityRoles.Owner,
            EmployeeType.Manager => IdentityRoles.Admin,
            EmployeeType.FrontDesk => IdentityRoles.FrontDesk,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        var kind = type.ToString().ToLowerInvariant();
        var identity = await CreateAccountAsync(
            userManager, $"sample.{kind}.org{organization.Id}.{sequence:000}@example.test", password, role);
        var common = new EmployeeFields(
            $"Sample{sequence:000}",
            type == EmployeeType.Manager ? "Manager" : "Staff",
            identity.Email!,
            $"555-{sequence:0000}",
            DateTime.UtcNow.AddDays(-sequence * 11),
            identity.Id);

        EmployeeBase employee = type switch
        {
            EmployeeType.Owner => new OwnerModel
            {
                FirstName = common.FirstName,
                LastName = common.LastName,
                Email = common.Email,
                PhoneNumber = common.PhoneNumber,
                HireDate = common.HireDate,
                IdentityUserId = common.IdentityUserId,
                OrganizationID = organization.Id
            },
            EmployeeType.Manager => new ManagerModel
            {
                FirstName = common.FirstName,
                LastName = common.LastName,
                Email = common.Email,
                PhoneNumber = common.PhoneNumber,
                HireDate = common.HireDate,
                IdentityUserId = common.IdentityUserId,
                PropteryId = property.Id
            },
            EmployeeType.FrontDesk => new FrontDeskAgentModel
            {
                FirstName = common.FirstName,
                LastName = common.LastName,
                Email = common.Email,
                PhoneNumber = common.PhoneNumber,
                HireDate = common.HireDate,
                IdentityUserId = common.IdentityUserId,
                PropteryId = property.Id,
                ShiftWorked =
                [
                    new Shift
                    {
                        Type = WorkShifts.Morning,
                        Start = new TimeOnly(7, 0),
                        End = new TimeOnly(15, 0)
                    }
                ]
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        database.Employees.Add(employee);
    }

    private static int CountEmployees(OrganizationModel organization) =>
        organization.Owners.Count +
        organization.Properties.Sum(property => property.Managers.Count + property.FrontDeskAgents.Count);

    private static async Task EnsureResidentsAsync(
        ShiftrDbContext database,
        UserManager<IdentityUser> userManager,
        OrganizationModel organization,
        string password,
        CancellationToken cancellationToken)
    {
        var residents = organization.Properties.SelectMany(property => property.Residents).ToList();
        var sequence = residents.Count;
        while (residents.Count < ResidentTarget)
        {
            sequence++;
            var email = $"sample.resident.org{organization.Id}.{sequence:000}@example.test";
            while (await userManager.FindByEmailAsync(email) is not null)
            {
                sequence++;
                email = $"sample.resident.org{organization.Id}.{sequence:000}@example.test";
            }

            var identity = await CreateAccountAsync(userManager, email, password, IdentityRoles.Resident);
            var resident = new ResidentModel
            {
                FirstName = SampleFirstNames[(sequence - 1) % SampleFirstNames.Length],
                LastName = SampleLastNames[(sequence - 1) % SampleLastNames.Length],
                UnitNumber = $"{100 + sequence / organization.Properties.Count}-{(sequence % 20) + 1:00}",
                AllowedGuests = sequence % 3 == 0 ? ["Taylor Guest", "Jordan Visitor"] : [],
                CallToNotify = sequence % 2 == 0,
                IdentityUserId = identity.Id,
                PropertyId = organization.Properties[(sequence - 1) % organization.Properties.Count].Id
            };
            database.Residents.Add(resident);
            residents.Add(resident);
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAmenitiesAsync(
        ShiftrDbContext database,
        PropertyModel property,
        CancellationToken cancellationToken)
    {
        foreach (var (name, description) in SampleAmenities)
        {
            if (property.Amenities.Any(amenity =>
                string.Equals(amenity.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var amenity = new AmenityTypeModel
            {
                PropertyId = property.Id,
                Name = name,
                Description = description
            };
            database.Amenities.Add(amenity);
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureReservationsAsync(
        ShiftrDbContext database,
        PropertyModel property,
        CancellationToken cancellationToken)
    {
        var residents = property.Residents.Where(resident => resident.IdentityUserId is not null).ToList();
        var amenities = property.Amenities;
        if (residents.Count == 0 || amenities.Count == 0) return;

        var currentCount = amenities.Sum(amenity => amenity.Reservations.Count);
        var timestamp = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(1);
        for (var index = currentCount; index < ReservationTargetPerProperty; index++)
        {
            var amenity = amenities[index % amenities.Count];
            var resident = residents[index % residents.Count];
            var startsAt = timestamp.AddDays(index).AddHours(9);
            while (amenity.Reservations.Any(reservation =>
                startsAt < reservation.EndsAt && startsAt.AddHours(1) > reservation.StartsAt))
            {
                startsAt = startsAt.AddDays(1);
            }

            var reservation = new AmenityReservationModel
            {
                AmenityTypeId = amenity.Id,
                ResidentIdentityUserId = resident.IdentityUserId!,
                StartsAt = startsAt,
                EndsAt = startsAt.AddHours(1),
                Notes = $"Sample data reservation {index + 1}"
            };
            database.AmenityReservations.Add(reservation);
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureShiftNotesAsync(
        ShiftrDbContext database,
        PropertyModel property,
        OrganizationModel organization,
        CancellationToken cancellationToken)
    {
        var author = (EmployeeBase?)property.Managers.FirstOrDefault() ??
            (EmployeeBase?)property.FrontDeskAgents.FirstOrDefault() ??
            organization.Owners.FirstOrDefault();
        if (author is null) return;

        var existing = await database.ShiftNotes
            .CountAsync(note => note.PropertyId == property.Id, cancellationToken);
        for (var index = existing; index < ShiftNoteTargetPerProperty; index++)
        {
            var updatedAt = DateTimeOffset.UtcNow.AddDays(-index);
            database.ShiftNotes.Add(new ShiftNoteModel
            {
                PropertyId = property.Id,
                AuthorEmployeeId = author.Id,
                Title = $"Sample shift handoff {index + 1}",
                Content = "Routine building handoff: deliveries are logged, shared areas are in good condition, and there are no urgent follow-ups.",
                CreatedAt = updatedAt.AddMinutes(-15),
                UpdatedAt = updatedAt
            });
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAccessRequestsAsync(
        ShiftrDbContext database,
        PropertyModel property,
        string password,
        UserManager<IdentityUser> userManager,
        CancellationToken cancellationToken)
    {
        foreach (var status in new[] { EmployeeAccessRequestStatus.Pending, EmployeeAccessRequestStatus.Rejected })
        {
            var kind = status.ToString().ToLowerInvariant();
            var identity = await GetOrCreateAccountAsync(
                userManager, $"sample.request.{kind}.property{property.Id}@example.test", password);
            if (await database.EmployeeAccessRequests.AnyAsync(
                request => request.PropertyId == property.Id && request.IdentityUserId == identity.Id,
                cancellationToken))
            {
                continue;
            }

            database.EmployeeAccessRequests.Add(new EmployeeAccessRequestModel
            {
                PropertyId = property.Id,
                IdentityUserId = identity.Id,
                FirstName = status == EmployeeAccessRequestStatus.Pending ? "Pending" : "Former",
                LastName = "Sample Applicant",
                Email = identity.Email!,
                PhoneNumber = "555-0199",
                Role = status == EmployeeAccessRequestStatus.Pending
                    ? EmployeeAccessRole.FrontDesk
                    : EmployeeAccessRole.Manager,
                Status = status,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(status == EmployeeAccessRequestStatus.Pending ? -1 : -14)
            });
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private static async Task<IdentityUser> CreateAccountAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        string role)
    {
        var user = await GetOrCreateAccountAsync(userManager, email, password);
        if (!await userManager.IsInRoleAsync(user, role))
        {
            var result = await userManager.AddToRoleAsync(user, role);
            EnsureSucceeded(result, $"assign role '{role}' to '{email}'");
        }

        return user;
    }

    private static async Task<IdentityUser> GetOrCreateAccountAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string password)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return existing;

        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        EnsureSucceeded(result, $"create development account '{email}'");
        return user;
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded) return;
        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Could not {operation}: {errors}");
    }

    private static readonly string[] SampleFirstNames =
    [
        "Avery", "Blake", "Casey", "Devon", "Emerson", "Finley", "Gray", "Harper",
        "Jamie", "Kai", "Logan", "Morgan", "Nico", "Parker", "Quinn", "Reese",
        "Riley", "Rowan", "Sage", "Taylor"
    ];

    private static readonly string[] SampleLastNames =
    [
        "Anderson", "Bennett", "Carter", "Diaz", "Ellis", "Foster", "Garcia", "Hayes",
        "Irwin", "Johnson", "Kim", "Lopez", "Miller", "Nguyen", "Owens", "Patel",
        "Reed", "Singh", "Turner", "Wilson"
    ];

    private sealed record EmployeeFields(
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        DateTime HireDate,
        string IdentityUserId);
}
