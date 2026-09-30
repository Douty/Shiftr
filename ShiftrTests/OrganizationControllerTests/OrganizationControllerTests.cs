using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public (int OrganizationId, int EmployeeId, int? PropertyId)? AddEmployeeRequest { get; private set; }

        public Task<OrganizationModel?> GetOrganizationById(int id) =>
            Task.FromResult(OrganizationToReturn);

        public Task<OrganizationModel> CreateOrganization(OrganizationModel organization)
        {
            OrganizationPassedToCreate = organization;
            return Task.FromResult(OrganizationToReturn ?? organization);
        }

        public Task<OrganizationModel?> UpdateOrganization(OrganizationModel organization) =>
            Task.FromResult(OrganizationToReturn);

        public Task<bool> DeleteOrganization(int id) => Task.FromResult(false);

        public Task<bool> AddEmployeeToOrganization(int organizationId, int employeeId, int? propertyId)
        {
            AddEmployeeRequest = (organizationId, employeeId, propertyId);
            return Task.FromResult(AddEmployeeResult);
        }

        public Task<PropertyModel?> AddPropertyToOrganization(int organizationId, PropertyModel property) =>
            Task.FromResult<PropertyModel?>(null);
    }

    [Theory]
    [InlineData(nameof(OrganizationController.GetOrganization), AuthorizationPolicies.RegularEmployee)]
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
    public async Task AddEmployee_PassesOrganizationEmployeeAndPropertyIds()
    {
        var service = new FakeOrganizationService { AddEmployeeResult = true };
        var controller = new OrganizationController(service);

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
        var organization = new OrganizationModel { Id = 12, Name = "Northstar" };
        var service = new FakeOrganizationService { OrganizationToReturn = organization };
        var controller = new OrganizationController(service);

        var result = await controller.CreateOrganization(organization);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(OrganizationController.GetOrganization), created.ActionName);
        Assert.Equal(12, created.RouteValues!["id"]);
        Assert.Same(organization, service.OrganizationPassedToCreate);
    }

    [Fact]
    public async Task AddProperty_ReturnsNotFoundWhenOrganizationDoesNotExist()
    {
        var controller = new OrganizationController(new FakeOrganizationService());

        var result = await controller.AddProperty(8, new PropertyModel { Name = "Main Street" });

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
