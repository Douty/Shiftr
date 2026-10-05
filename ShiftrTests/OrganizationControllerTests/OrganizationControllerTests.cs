using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Shiftr.Controllers;
using Shiftr.DTOs;
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Security;

namespace ShiftrTests;

[Trait("Category", "Unit")]
public class OrganizationControllerTests
{
    private sealed class FakeOrganizationService : IOrganizationService
    {
        public OrganizationModel? OrganizationToReturn { get; init; }
        public OrganizationModel? OrganizationPassedToCreate { get; private set; }
        public bool AddEmployeeResult { get; init; }
        public OrganizationDeleteResult DeleteResult { get; init; } = OrganizationDeleteResult.NotFound;
        public bool HasMemberResult { get; init; }
        public bool HasAdminAccessResult { get; init; }
        public bool HasOwnerAccessResult { get; init; }
        public (int OrganizationId, int EmployeeId, int? PropertyId)? AddEmployeeRequest { get; private set; }

        public Task<OrganizationModel?> GetOrganizationById(int id) =>
            Task.FromResult(OrganizationToReturn);

        public string? IdentityUserIdPassedToList { get; private set; }

        public Task<List<OrganizationModel>> GetOrganizationsForUser(string identityUserId)
        {
            IdentityUserIdPassedToList = identityUserId;
            return Task.FromResult(OrganizationToReturn is null ? new List<OrganizationModel>() : new List<OrganizationModel> { OrganizationToReturn });
        }

        public Task<OrganizationModel> CreateOrganization(OrganizationModel organization)
        {
            OrganizationPassedToCreate = organization;
            return Task.FromResult(OrganizationToReturn ?? organization);
        }

        public Task<OrganizationModel?> UpdateOrganization(OrganizationModel organization) =>
            Task.FromResult(OrganizationToReturn);

        public Task<OrganizationDeleteResult> DeleteOrganization(int id) => Task.FromResult(DeleteResult);

        public Task<bool> AddEmployeeToOrganization(int organizationId, int employeeId, int? propertyId)
        {
            AddEmployeeRequest = (organizationId, employeeId, propertyId);
            return Task.FromResult(AddEmployeeResult);
        }

        public Task<PropertyModel?> AddPropertyToOrganization(int organizationId, PropertyModel property) =>
            Task.FromResult<PropertyModel?>(null);

        public Task<bool> HasMember(int organizationId, string identityUserId) => Task.FromResult(HasMemberResult);

        public Task<bool> HasAdminAccess(int organizationId, string identityUserId) => Task.FromResult(HasAdminAccessResult);

        public Task<bool> HasOwnerAccess(int organizationId, string identityUserId) => Task.FromResult(HasOwnerAccessResult);
    }

    private static OrganizationController CreateController(IOrganizationService service)
    {
        var controller = new OrganizationController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "identity-1")], "test"))
            }
        };
        return controller;
    }

    [Theory]
    [InlineData(nameof(OrganizationController.GetOrganization), AuthorizationPolicies.RegularEmployee)]
    [InlineData(nameof(OrganizationController.GetMyOrganizations), AuthorizationPolicies.AdminOnly)]
    [InlineData(nameof(OrganizationController.CreateOrganization), AuthorizationPolicies.OwnerOnly)]
    [InlineData(nameof(OrganizationController.UpdateOrganization), AuthorizationPolicies.OwnerOnly)]
    [InlineData(nameof(OrganizationController.DeleteOrganization), AuthorizationPolicies.OwnerOnly)]
    [InlineData(nameof(OrganizationController.AddEmployee), AuthorizationPolicies.AdminOnly)]
    [InlineData(nameof(OrganizationController.AddProperty), AuthorizationPolicies.AdminOnly)]
    public void Actions_RequireExpectedPolicy(string actionName, string expectedPolicy)
    {
        var action = typeof(OrganizationController).GetMethod(actionName);
        var authorization = action!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(expectedPolicy, authorization.Policy);
    }

    [Fact]
    public async Task GetMyOrganizations_QueriesForSignedInIdentity()
    {
        var organization = new OrganizationModel { Id = 8, Name = "Northstar" };
        var service = new FakeOrganizationService { OrganizationToReturn = organization };
        var controller = CreateController(service);

        var result = await controller.GetMyOrganizations();

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("identity-1", service.IdentityUserIdPassedToList);
        Assert.Equal(new[] { organization }, Assert.IsType<List<OrganizationModel>>(response.Value));
    }

    [Fact]
    public async Task AddEmployee_PassesOrganizationEmployeeAndPropertyIds()
    {
        var service = new FakeOrganizationService { AddEmployeeResult = true };
        var controller = CreateController(service);
        service = new FakeOrganizationService
        {
            AddEmployeeResult = true,
            HasAdminAccessResult = true,
            OrganizationToReturn = new OrganizationModel { Id = 8, Name = "Northstar" }
        };
        controller = CreateController(service);

        var result = await controller.AddEmployee(8, new AddOrganizationEmployeeRequest
        {
            EmployeeId = 21,
            PropertyId = 34
        });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal((8, 21, 34), service.AddEmployeeRequest);
    }

    [Fact]
    public async Task CreateOrganization_ReturnsCreatedAtGetAction()
    {
        var organization = new OrganizationModel
        {
            Id = 12,
            Name = "Northstar",
            Owners = [new OwnerModel
            {
                FirstName = "Casey",
                LastName = "Owner",
                Email = "owner@example.com",
                PhoneNumber = "555-0101",
                OrganizationID = 12,
                IdentityUserId = "identity-1"
            }]
        };
        var service = new FakeOrganizationService { OrganizationToReturn = organization };
        var controller = CreateController(service);

        var result = await controller.CreateOrganization(organization);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(OrganizationController.GetOrganization), created.ActionName);
        Assert.Equal(12, created.RouteValues!["id"]);
        Assert.Same(organization, service.OrganizationPassedToCreate);
    }

    [Fact]
    public async Task AddProperty_ReturnsNotFoundWhenOrganizationDoesNotExist()
    {
        var controller = CreateController(new FakeOrganizationService());

        var result = await controller.AddProperty(8, new PropertyModel { Name = "Main Street" });

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteOrganization_ReturnsConflict_WhenDependentsExist()
    {
        var controller = CreateController(new FakeOrganizationService
        {
            DeleteResult = OrganizationDeleteResult.HasDependents,
            OrganizationToReturn = new OrganizationModel { Id = 8, Name = "Northstar" },
            HasOwnerAccessResult = true
        });

        var result = await controller.DeleteOrganization(8);

        Assert.IsType<ConflictResult>(result);
    }
}
