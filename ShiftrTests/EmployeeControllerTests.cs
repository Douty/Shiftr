using Microsoft.AspNetCore.Mvc;
using Shiftr.Controllers;
using Shiftr.Interface;
using Shiftr.Models;

namespace ShiftrTests;

public class EmployeeControllerTests
{
    [Fact]
    public async Task GetEmployee_ReturnsOk_WhenEmployeeExists()
    {
        var employee = CreateManager();
        var service = new FakeEmployeeService { EmployeeToReturn = employee };
        var controller = new EmployeeController(service);

        var result = await controller.GetEmployee(employee.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(employee, okResult.Value);
        Assert.Equal(employee.Id, service.RequestedEmployeeId);
    }

    [Fact]
    public async Task GetEmployee_ReturnsNotFound_WhenEmployeeDoesNotExist()
    {
        var service = new FakeEmployeeService();
        var controller = new EmployeeController(service);

        var result = await controller.GetEmployee(404);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Equal(404, service.RequestedEmployeeId);
    }

    [Fact]
    public async Task CreateEmployee_ReturnsCreatedAtAction_WithCreatedEmployee()
    {
        var employee = CreateManager();
        var service = new FakeEmployeeService { EmployeeToReturn = employee };
        var controller = new EmployeeController(service);

        var result = await controller.CreateEmployee(employee);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(EmployeeController.GetEmployee), createdResult.ActionName);
        Assert.Equal(employee.Id, createdResult.RouteValues!["id"]);
        Assert.Same(employee, createdResult.Value);
        Assert.Same(employee, service.EmployeePassedToCreate);
    }

    [Fact]
    public async Task DeleteEmployee_ReturnsServiceResult_AndPassesEmployeeId()
    {
        var service = new FakeEmployeeService { DeleteResult = true };
        var controller = new EmployeeController(service);

        var result = await controller.DeleteEmployee(17);

        Assert.True(result.Value);
        Assert.Equal(17, service.RequestedEmployeeId);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsUpdatedEmployee()
    {
        var employee = CreateManager();
        var service = new FakeEmployeeService { EmployeeToReturn = employee };
        var controller = new EmployeeController(service);

        var result = await controller.UpdateEmployee(employee);

        Assert.Same(employee, result.Value);
        Assert.Same(employee, service.EmployeePassedToUpdate);
    }

    [Theory]
    [InlineData(EmployeeType.Manager, true)]
    [InlineData(EmployeeType.Owner, true)]
    [InlineData(EmployeeType.FrontDesk, false)]
    public void IsAdmin_ReturnsServiceResult(EmployeeType employeeType, bool expected)
    {
        var employee = CreateEmployee(employeeType);
        var controller = new EmployeeController(new FakeEmployeeService());

        var result = controller.IsAdmin(employee);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Theory]
    [InlineData(EmployeeType.Owner, true)]
    [InlineData(EmployeeType.Manager, false)]
    [InlineData(EmployeeType.FrontDesk, false)]
    public void IsOwner_ReturnsServiceResult(EmployeeType employeeType, bool expected)
    {
        var employee = CreateEmployee(employeeType);
        var controller = new EmployeeController(new FakeEmployeeService());

        var result = controller.IsOwner(employee);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

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

        public Task<EmployeeBase?> GetEmployeeById(int Id)
        {
            RequestedEmployeeId = Id;
            return Task.FromResult(EmployeeToReturn);
        }

        public Task<EmployeeBase> CreateEmployee(EmployeeBase Employee)
        {
            EmployeePassedToCreate = Employee;
            return Task.FromResult(EmployeeToReturn ?? Employee);
        }

        public Task<EmployeeBase> UpdateEmployee(EmployeeBase Employee)
        {
            EmployeePassedToUpdate = Employee;
            return Task.FromResult(EmployeeToReturn ?? Employee);
        }

        public Task<bool> DeleteEmployee(int Id)
        {
            RequestedEmployeeId = Id;
            return Task.FromResult(DeleteResult);
        }

        public bool IsAdmin(EmployeeBase Employee) =>
            Employee.Type is EmployeeType.Manager or EmployeeType.Owner;

        public bool IsOwner(EmployeeBase Employee) => Employee.Type == EmployeeType.Owner;
    }
}
