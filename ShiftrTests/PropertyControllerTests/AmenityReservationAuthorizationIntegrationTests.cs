using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shiftr.Data;
using Shiftr.Interface;
using Shiftr.Models;
using ShiftrTests.fixture;

namespace ShiftrTests;

[Trait("Category", "Integration")]
public sealed class AmenityReservationAuthorizationIntegrationTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public AmenityReservationAuthorizationIntegrationTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReservationAccessRequiresFrontDeskAssignmentToExactProperty()
    {
        int assignedPropertyId;
        int otherPropertyId;
        string identityUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ShiftrDbContext>();
            var identityUser = new IdentityUser
            {
                UserName = $"frontdesk-{Guid.NewGuid():N}@example.com",
                Email = $"frontdesk-{Guid.NewGuid():N}@example.com"
            };
            database.Users.Add(identityUser);

            var organization = new OrganizationModel
            {
                Name = $"Amenity authorization {Guid.NewGuid():N}"
            };
            var assignedProperty = new PropertyModel { Name = "Assigned property" };
            var otherProperty = new PropertyModel { Name = "Other property" };
            organization.Properties.Add(assignedProperty);
            organization.Properties.Add(otherProperty);
            database.Organizations.Add(organization);
            await database.SaveChangesAsync();

            database.Employees.Add(new FrontDeskAgentModel
            {
                FirstName = "Casey",
                LastName = "Morgan",
                Email = identityUser.Email!,
                PhoneNumber = "555-0100",
                PropteryId = assignedProperty.Id,
                IdentityUserId = identityUser.Id
            });
            await database.SaveChangesAsync();

            assignedPropertyId = assignedProperty.Id;
            otherPropertyId = otherProperty.Id;
            identityUserId = identityUser.Id;
        }

        using var serviceScope = _factory.Services.CreateScope();
        var propertyService = serviceScope.ServiceProvider.GetRequiredService<IPropertyService>();

        Assert.True(await propertyService.CanManageReservations(assignedPropertyId, identityUserId));
        Assert.False(await propertyService.CanManageReservations(otherPropertyId, identityUserId));
    }

    [Fact]
    public async Task ReservationServiceRejectsOverlappingBookingsAndNormalizesOffsets()
    {
        int propertyId;
        string residentIdentityUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ShiftrDbContext>();
            var property = new PropertyModel { Name = $"Reservation test {Guid.NewGuid():N}" };
            var resident = new IdentityUser
            {
                UserName = $"resident-{Guid.NewGuid():N}@example.com",
                Email = $"resident-{Guid.NewGuid():N}@example.com"
            };
            database.Properties.Add(property);
            database.Users.Add(resident);
            await database.SaveChangesAsync();
            propertyId = property.Id;
            residentIdentityUserId = resident.Id;
        }

        using var serviceScope = _factory.Services.CreateScope();
        var amenityService = serviceScope.ServiceProvider.GetRequiredService<IAmenityService>();
        var (amenity, nameConflict) = await amenityService.CreateAmenity(propertyId, "Pool", null);
        Assert.False(nameConflict);
        Assert.NotNull(amenity);

        var startsAt = new DateTimeOffset(2026, 10, 2, 14, 0, 0, TimeSpan.FromHours(2));
        var endsAt = startsAt.AddHours(1);
        var (firstResult, firstReservation) = await amenityService.CreateReservation(
            propertyId, amenity!.Id, residentIdentityUserId, startsAt, endsAt, null);
        var (overlapResult, _) = await amenityService.CreateReservation(
            propertyId, amenity.Id, residentIdentityUserId, startsAt.AddMinutes(30), endsAt.AddMinutes(30), null);
        var (adjacentResult, adjacentReservation) = await amenityService.CreateReservation(
            propertyId, amenity.Id, residentIdentityUserId, endsAt, endsAt.AddHours(1), null);

        Assert.Equal(AmenityReservationResult.Success, firstResult);
        Assert.Equal(TimeSpan.Zero, firstReservation!.StartsAt.Offset);
        Assert.Equal(AmenityReservationResult.TimeConflict, overlapResult);
        Assert.Equal(AmenityReservationResult.Success, adjacentResult);
        Assert.NotNull(adjacentReservation);
    }

    [Fact]
    public async Task ResidentsCanOnlyViewUpdateAndCancelTheirOwnReservations()
    {
        int propertyId;
        string firstResidentIdentityUserId;
        string secondResidentIdentityUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ShiftrDbContext>();
            var property = new PropertyModel { Name = $"Resident booking access {Guid.NewGuid():N}" };
            var firstResident = new IdentityUser
            {
                UserName = $"resident-one-{Guid.NewGuid():N}@example.com",
                Email = $"resident-one-{Guid.NewGuid():N}@example.com"
            };
            var secondResident = new IdentityUser
            {
                UserName = $"resident-two-{Guid.NewGuid():N}@example.com",
                Email = $"resident-two-{Guid.NewGuid():N}@example.com"
            };
            database.Properties.Add(property);
            database.Users.AddRange(firstResident, secondResident);
            await database.SaveChangesAsync();
            propertyId = property.Id;
            firstResidentIdentityUserId = firstResident.Id;
            secondResidentIdentityUserId = secondResident.Id;
        }

        using var serviceScope = _factory.Services.CreateScope();
        var amenityService = serviceScope.ServiceProvider.GetRequiredService<IAmenityService>();
        var (amenity, nameConflict) = await amenityService.CreateAmenity(propertyId, "Community room", null);
        Assert.False(nameConflict);
        Assert.NotNull(amenity);

        var startsAt = new DateTimeOffset(2026, 10, 3, 14, 0, 0, TimeSpan.Zero);
        var (firstResult, firstReservation) = await amenityService.CreateReservation(
            propertyId, amenity!.Id, firstResidentIdentityUserId, startsAt, startsAt.AddHours(1), null);
        var (secondResult, secondReservation) = await amenityService.CreateReservation(
            propertyId, amenity.Id, secondResidentIdentityUserId, startsAt.AddHours(2), startsAt.AddHours(3), null);

        Assert.Equal(AmenityReservationResult.Success, firstResult);
        Assert.Equal(AmenityReservationResult.Success, secondResult);
        Assert.Single(await amenityService.GetReservationsForResident(propertyId, firstResidentIdentityUserId));

        var (unauthorizedUpdateResult, _) = await amenityService.UpdateResidentReservation(
            propertyId,
            secondReservation!.Id,
            amenity.Id,
            firstResidentIdentityUserId,
            startsAt.AddHours(4),
            startsAt.AddHours(5),
            null);
        var deletedAnotherResidentReservation = await amenityService.DeleteResidentReservation(
            propertyId, secondReservation.Id, firstResidentIdentityUserId);
        var (updateResult, updatedReservation) = await amenityService.UpdateResidentReservation(
            propertyId,
            firstReservation!.Id,
            amenity.Id,
            firstResidentIdentityUserId,
            startsAt.AddHours(4),
            startsAt.AddHours(5),
            "Updated by resident");
        var deletedOwnReservation = await amenityService.DeleteResidentReservation(
            propertyId, firstReservation.Id, firstResidentIdentityUserId);

        Assert.Equal(AmenityReservationResult.NotFound, unauthorizedUpdateResult);
        Assert.False(deletedAnotherResidentReservation);
        Assert.Equal(AmenityReservationResult.Success, updateResult);
        Assert.Equal("Updated by resident", updatedReservation!.Notes);
        Assert.True(deletedOwnReservation);
    }
}