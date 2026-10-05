using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shiftr.Controllers;
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Security;

namespace ShiftrTests;

[Trait("Category", "Unit")]
public class EmployeeControllerUnitTests
{
    private static EmployeeBase CreateManager() => new ManagerModel
    {
        Id = 17,
        FirstName = "Taylor",
        LastName = "Reed",
        Email = "taylor@example.com",
        PhoneNumber = "555-0100",
        PropteryId = 1
    };

    private static EmployeeBase CreateEmployee(EmployeeType type) => type switch
    {
        EmployeeType.Owner => new OwnerModel
        {
            FirstName = "Taylor",
            LastName = "Reed",
            Email = "taylor@example.com",
            PhoneNumber = "555-0100",
            OrganizationID = 1
        },
        EmployeeType.Manager => CreateManager(),
        _ => new FrontDeskAgentModel
        {
            FirstName = "Taylor",
            LastName = "Reed",
            Email = "taylor@example.com",
            PhoneNumber = "555-0100",
            PropteryId = 1
        }
    };

    private sealed class FakeEmployeeService : IEmployeeService
    {
        public EmployeeBase? EmployeeToReturn { get; init; }
        public bool DeleteResult { get; init; }
        public int? RequestedEmployeeId { get; private set; }
        public EmployeeBase? EmployeePassedToCreate { get; private set; }
        public EmployeeBase? EmployeePassedToUpdate { get; private set; }

        public Task<EmployeeBase?> GetEmployeeById(int id)
        {
            RequestedEmployeeId = id;
            return Task.FromResult(EmployeeToReturn);
        }

        public Task<EmployeeBase> CreateEmployee(EmployeeBase employee)
        {
            EmployeePassedToCreate = employee;
            return Task.FromResult(EmployeeToReturn ?? employee);
        }

        public Task<EmployeeBase?> UpdateEmployee(EmployeeBase employee)
        {
            EmployeePassedToUpdate = employee;
            return Task.FromResult<EmployeeBase?>(EmployeeToReturn);
        }

        public Task<bool> DeleteEmployee(int id)
        {
            RequestedEmployeeId = id;
            return Task.FromResult(DeleteResult);
        }

        public bool IsAdmin(EmployeeBase employee) =>
            employee.Type is EmployeeType.Manager or EmployeeType.Owner;

        public bool IsOwner(EmployeeBase employee) => employee.Type == EmployeeType.Owner;

        public Task<bool> CanAccessEmployee(int employeeId, string identityUserId) => Task.FromResult(true);

        public Task<bool> HasEmployeeProfile(string identityUserId) => Task.FromResult(EmployeeToReturn is not null);

        public Task<bool> CanCreateEmployee(EmployeeBase employee, string identityUserId) => Task.FromResult(true);
    }

    private static EmployeeController CreateController(IEmployeeService service, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "identity-1") };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var controller = new EmployeeController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task CreateEmployee_ReturnsCreatedAtAction_WithCreatedEmployee()
    {
        var employee = CreateManager();
        var service = new FakeEmployeeService { EmployeeToReturn = employee };
        var controller = CreateController(service);

        var result = await controller.CreateEmployee(employee);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(EmployeeController.GetEmployee), createdResult.ActionName);
        Assert.Equal(employee.Id, createdResult.RouteValues!["id"]);
        Assert.Same(employee, createdResult.Value);
        Assert.Same(employee, service.EmployeePassedToCreate);
    }

    [Fact]
    public async Task GetEmployee_ReturnsOk_WhenEmployeeExists()
    {
        var employee = CreateManager();
        var service = new FakeEmployeeService { EmployeeToReturn = employee };
        var controller = CreateController(service);

        var result = await controller.GetEmployee(employee.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(employee, okResult.Value);
        Assert.Equal(employee.Id, service.RequestedEmployeeId);
    }

    [Fact]
    public async Task GetEmployee_ReturnsNotFound_WhenEmployeeDoesNotExist()
    {
        var service = new FakeEmployeeService();
        var controller = CreateController(service);

        var result = await controller.GetEmployee(404);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Equal(404, service.RequestedEmployeeId);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsUpdatedEmployee()
    {
        var employee = CreateManager();
        var service = new FakeEmployeeService { EmployeeToReturn = employee };
        var controller = CreateController(service);

        var result = await controller.UpdateEmployee(employee);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(employee, okResult.Value);
        Assert.Same(employee, service.EmployeePassedToUpdate);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsNotFound_WhenEmployeeDoesNotExist()
    {
        var service = new FakeEmployeeService();
        var controller = CreateController(service);

        var result = await controller.UpdateEmployee(CreateManager());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteEmployee_ReturnsServiceResult_AndPassesEmployeeId()
    {
        var service = new FakeEmployeeService
        {
            DeleteResult = true,
            EmployeeToReturn = CreateManager()
        };
        var controller = CreateController(service);

        var result = await controller.DeleteEmployee(17);

        Assert.True(result.Value);
        Assert.Equal(17, service.RequestedEmployeeId);
    }

    [Theory]
    [InlineData(EmployeeType.Manager, true)]
    [InlineData(EmployeeType.Owner, true)]
    [InlineData(EmployeeType.FrontDesk, false)]
    public void IsAdmin_ReturnsServiceResult(EmployeeType employeeType, bool expected)
    {
        var controller = CreateController(
            new FakeEmployeeService(),
            employeeType == EmployeeType.FrontDesk ? IdentityRoles.FrontDesk : IdentityRoles.Admin);

        var result = controller.IsAdmin();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Theory]
    [InlineData(EmployeeType.Owner, true)]
    [InlineData(EmployeeType.Manager, false)]
    [InlineData(EmployeeType.FrontDesk, false)]
    public void IsOwner_ReturnsServiceResult(EmployeeType employeeType, bool expected)
    {
        var controller = CreateController(
            new FakeEmployeeService(),
            employeeType == EmployeeType.Owner ? IdentityRoles.Owner : IdentityRoles.Admin);

        var result = controller.IsOwner();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }
}
