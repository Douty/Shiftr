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

    [Fact]
    public void ReservationControllerRequiresFrontDeskRole()
    {
        var authorization = typeof(AmenityReservationsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(IdentityRoles.FrontDesk, authorization.Roles);
    }
}