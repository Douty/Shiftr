using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Shiftr.Controllers;
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Security;

namespace ShiftrTests;

[Trait("Category", "Unit")]
public class PropertyControllerTests
{
    private sealed class FakePropertyService : IPropertyService
    {
        public PropertyModel? PropertyToReturn { get; init; }
        public PropertyModel? CreatedProperty { get; init; }
        public PropertyModel? UpdatedProperty { get; init; }
        public PropertyDeleteResult DeleteResult { get; init; } = PropertyDeleteResult.NotFound;
        public bool CanAccessPropertyResult { get; init; }
        public bool CanManagePropertyResult { get; init; }
        public bool CanManageOrganizationResult { get; init; }
        public bool CanManageReservationsResult { get; init; }
        public int? CreatedOrganizationId { get; private set; }
        public PropertyModel? PropertyPassedToCreate { get; private set; }
        public PropertyModel? PropertyPassedToUpdate { get; private set; }
        public int? DeletedPropertyId { get; private set; }
        public (int Id, PropertyInviteType Type)? RotatedInvite { get; private set; }
        public string? RotatedInviteId { get; init; }

        public Task<PropertyModel?> GetPropertyById(int id) => Task.FromResult(PropertyToReturn);

        public Task<bool> IsInviteCodeValidAsync(string inviteCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<PropertyModel?> CreateProperty(int organizationId, PropertyModel property)
        {
            CreatedOrganizationId = organizationId;
            PropertyPassedToCreate = property;
            return Task.FromResult(CreatedProperty);
        }

        public Task<PropertyModel?> UpdateProperty(PropertyModel property)
        {
            PropertyPassedToUpdate = property;
            return Task.FromResult(UpdatedProperty);
        }

        public Task<string?> RotateInviteId(int id, PropertyInviteType inviteType)
        {
            RotatedInvite = (id, inviteType);
            return Task.FromResult(RotatedInviteId);
        }

        public Task<PropertyDeleteResult> DeleteProperty(int id)
        {
            DeletedPropertyId = id;
            return Task.FromResult(DeleteResult);
        }

        public Task<bool> CanAccessProperty(int propertyId, string identityUserId) =>
            Task.FromResult(CanAccessPropertyResult);

        public Task<bool> CanManageProperty(int propertyId, string identityUserId) =>
            Task.FromResult(CanManagePropertyResult);

        public Task<bool> CanManageOrganization(int organizationId, string identityUserId) =>
            Task.FromResult(CanManageOrganizationResult);

        public Task<bool> CanManageReservations(int propertyId, string identityUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CanManageReservationsResult);
    }

    private static PropertyController CreateController(IPropertyService service, bool authenticated = true)
    {
        var claims = authenticated
            ? new[] { new Claim(ClaimTypes.NameIdentifier, "identity-1") }
            : Array.Empty<Claim>();
        var controller = new PropertyController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? "test" : null))
            }
        };
        return controller;
    }

    [Theory]
    [InlineData(nameof(PropertyController.GetProperty), AuthorizationPolicies.RegularEmployee)]
    [InlineData(nameof(PropertyController.CreateProperty), AuthorizationPolicies.AdminOnly)]
    [InlineData(nameof(PropertyController.UpdateProperty), AuthorizationPolicies.AdminOnly)]
    [InlineData(nameof(PropertyController.RotateInviteId), AuthorizationPolicies.AdminOnly)]
    [InlineData(nameof(PropertyController.DeleteProperty), AuthorizationPolicies.AdminOnly)]
    public void Actions_RequireExpectedPolicy(string actionName, string expectedPolicy)
    {
        var action = typeof(PropertyController).GetMethod(actionName);
        var authorization = action!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(expectedPolicy, authorization.Policy);
    }

    [Fact]
    public void PropertyInviteIds_AreGeneratedAndRotateIndependently()
    {
        var property = new PropertyModel { Name = "Main Street" };
        var originalResidentInviteId = property.ResidentInviteId;
        var originalEmployeeInviteId = property.EmployeeInviteId;

        var rotatedInviteId = property.RotateInviteId(PropertyInviteType.Resident);

        Assert.NotEqual(originalResidentInviteId, originalEmployeeInviteId);
        Assert.NotEqual(originalResidentInviteId, rotatedInviteId);
        Assert.Equal(rotatedInviteId, property.ResidentInviteId);
        Assert.Equal(originalEmployeeInviteId, property.EmployeeInviteId);
    }

    [Fact]
    public async Task GetProperty_ReturnsNotFoundWhenPropertyDoesNotExist()
    {
        var controller = CreateController(new FakePropertyService());

        var result = await controller.GetProperty(14);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetProperty_ForbidsNonMember()
    {
        var controller = CreateController(new FakePropertyService
        {
            PropertyToReturn = new PropertyModel { Id = 14, Name = "Main Street" }
        });

        var result = await controller.GetProperty(14);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetProperty_ReturnsPropertyForMember()
    {
        var property = new PropertyModel { Id = 14, Name = "Main Street" };
        var controller = CreateController(new FakePropertyService
        {
            PropertyToReturn = property,
            CanAccessPropertyResult = true
        });

        var result = await controller.GetProperty(14);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(property, response.Value);
    }

    [Fact]
    public async Task CreateProperty_ReturnsCreatedAtGetAndPassesOrganizationId()
    {
        var property = new PropertyModel { Id = 14, Name = "Main Street" };
        var service = new FakePropertyService
        {
            CreatedProperty = property,
            CanManageOrganizationResult = true
        };
        var controller = CreateController(service);

        var result = await controller.CreateProperty(8, property);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(PropertyController.GetProperty), created.ActionName);
        Assert.Equal(14, created.RouteValues!["id"]);
        Assert.Equal(8, service.CreatedOrganizationId);
        Assert.Same(property, service.PropertyPassedToCreate);
    }

    [Fact]
    public async Task CreateProperty_ForbidsNonAdmin()
    {
        var service = new FakePropertyService();
        var controller = CreateController(service);

        var result = await controller.CreateProperty(8, new PropertyModel { Name = "Main Street" });

        Assert.IsType<ForbidResult>(result.Result);
        Assert.Null(service.PropertyPassedToCreate);
    }

    [Fact]
    public async Task CreateProperty_ReturnsNotFoundWhenOrganizationDoesNotExist()
    {
        var controller = CreateController(new FakePropertyService { CanManageOrganizationResult = true });

        var result = await controller.CreateProperty(8, new PropertyModel { Name = "Main Street" });

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateProperty_ReturnsBadRequestWhenRouteAndModelIdsDiffer()
    {
        var service = new FakePropertyService();
        var controller = CreateController(service);

        var result = await controller.UpdateProperty(14, new PropertyModel { Id = 15, Name = "Updated" });

        Assert.IsType<BadRequestResult>(result.Result);
        Assert.Null(service.PropertyPassedToUpdate);
    }

    [Fact]
    public async Task UpdateProperty_UpdatesPropertyForAdmin()
    {
        var property = new PropertyModel { Id = 14, Name = "Updated" };
        var service = new FakePropertyService
        {
            PropertyToReturn = property,
            UpdatedProperty = property,
            CanManagePropertyResult = true
        };
        var controller = CreateController(service);

        var result = await controller.UpdateProperty(14, property);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(property, response.Value);
        Assert.Same(property, service.PropertyPassedToUpdate);
    }

    [Fact]
    public async Task RotateInviteId_RotatesSelectedInviteForAdmin()
    {
        var service = new FakePropertyService
        {
            PropertyToReturn = new PropertyModel { Id = 14, Name = "Main Street" },
            CanManagePropertyResult = true,
            RotatedInviteId = "new-invite-id"
        };
        var controller = CreateController(service);

        var result = await controller.RotateInviteId(14, PropertyInviteType.Resident);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Equal((14, PropertyInviteType.Resident), service.RotatedInvite);
        Assert.Equal("new-invite-id", response.Value!.GetType().GetProperty("inviteId")!.GetValue(response.Value));
    }

    [Fact]
    public async Task RotateInviteId_ForbidsNonAdmin()
    {
        var service = new FakePropertyService
        {
            PropertyToReturn = new PropertyModel { Id = 14, Name = "Main Street" }
        };
        var controller = CreateController(service);

        var result = await controller.RotateInviteId(14, PropertyInviteType.Employee);

        Assert.IsType<ForbidResult>(result);
        Assert.Null(service.RotatedInvite);
    }

    [Fact]
    public async Task RotateInviteId_ReturnsNotFoundForMissingProperty()
    {
        var service = new FakePropertyService();
        var controller = CreateController(service);

        var result = await controller.RotateInviteId(14, PropertyInviteType.Employee);

        Assert.IsType<NotFoundResult>(result);
        Assert.Null(service.RotatedInvite);
    }

    [Theory]
    [InlineData(PropertyDeleteResult.Deleted, typeof(NoContentResult))]
    [InlineData(PropertyDeleteResult.NotFound, typeof(NotFoundResult))]
    [InlineData(PropertyDeleteResult.HasEmployees, typeof(ConflictResult))]
    [InlineData(PropertyDeleteResult.HasReservations, typeof(ConflictResult))]
    public async Task DeleteProperty_MapsRepositoryResultToHttpResult(
        PropertyDeleteResult deleteResult,
        Type expectedResultType)
    {
        var service = new FakePropertyService
        {
            PropertyToReturn = new PropertyModel { Id = 14, Name = "Main Street" },
            CanManagePropertyResult = true,
            DeleteResult = deleteResult
        };
        var controller = CreateController(service);

        var result = await controller.DeleteProperty(14);

        Assert.IsType(expectedResultType, result);
        Assert.Equal(14, service.DeletedPropertyId);
    }

    [Fact]
    public async Task DeleteProperty_ForbidsNonAdmin()
    {
        var service = new FakePropertyService
        {
            PropertyToReturn = new PropertyModel { Id = 14, Name = "Main Street" }
        };
        var controller = CreateController(service);

        var result = await controller.DeleteProperty(14);

        Assert.IsType<ForbidResult>(result);
        Assert.Null(service.DeletedPropertyId);
    }
}