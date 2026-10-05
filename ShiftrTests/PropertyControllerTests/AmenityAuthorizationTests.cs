using Microsoft.AspNetCore.Authorization;
using Shiftr.Controllers;
using Shiftr.Security;

namespace ShiftrTests;

[Trait("Category", "Unit")]
public class AmenityAuthorizationTests
{
    [Theory]
    [InlineData(nameof(AmenitiesController.CreateAmenity))]
    [InlineData(nameof(AmenitiesController.UpdateAmenity))]
    [InlineData(nameof(AmenitiesController.DeleteAmenity))]
    public void AmenityMutationsRequireAdminPolicy(string actionName)
    {
        var action = typeof(AmenitiesController).GetMethod(actionName);
        var authorization = action!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(AuthorizationPolicies.AdminOnly, authorization.Policy);
    }

    [Theory]
    [InlineData(nameof(AmenityReservationsController.CreateReservation))]
    [InlineData(nameof(AmenityReservationsController.UpdateReservation))]
    [InlineData(nameof(AmenityReservationsController.DeleteReservation))]
    public void ReservationMutationsAllowOwnersFrontDeskAndAdmins(string actionName)
    {
        var action = typeof(AmenityReservationsController).GetMethod(actionName);
        var authorization = action!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(
            string.Join(",", IdentityRoles.Owner, IdentityRoles.FrontDesk, IdentityRoles.Admin),
            authorization.Roles);
    }
}