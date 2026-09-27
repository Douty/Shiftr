using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shiftr.Data;
using Shiftr.Models;
using ShiftrTests.fixture;

namespace ShiftrTests;

public sealed class EmployeeControllerIntegrationTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public EmployeeControllerIntegrationTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateEmployee_ReturnsCreatedAndEmployeeCanBeRetrieved()
    {
        await ResetEmployeesAsync();
        using var client = _factory.CreateClient();
        using var createResponse = await client.PostAsJsonAsync<EmployeeBase>(
            "/api/Employee/Create", CreateManager());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var employeeId = createdDocument.RootElement.GetProperty("id").GetInt32();

        using var getResponse = await client.GetAsync($"/api/Employee/{employeeId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        using var employeeDocument = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal("Taylor", employeeDocument.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("Reed", employeeDocument.RootElement.GetProperty("lastName").GetString());
    }

    [Fact]
    public async Task GetEmployee_ReturnsNotFoundWhenEmployeeDoesNotExist()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/Employee/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_PersistsUpdatedEmployee()
    {
        await ResetEmployeesAsync();
        using var client = _factory.CreateClient();
        var employee = await CreateManagerAsync(client);
        employee.LastName = "Morgan";

        using var updateResponse = await client.PostAsJsonAsync<EmployeeBase>("/api/Employee/Update", employee);
        using var getResponse = await client.GetAsync($"/api/Employee/{employee.Id}");

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        using var employeeDocument = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal("Morgan", employeeDocument.RootElement.GetProperty("lastName").GetString());
    }

    [Fact]
    public async Task DeleteEmployee_RemovesEmployee()
    {
        await ResetEmployeesAsync();
        using var client = _factory.CreateClient();
        var employee = await CreateManagerAsync(client);

        using var deleteResponse = await client.DeleteAsync($"/api/Employee/Delete/{employee.Id}");
        using var getResponse = await client.GetAsync($"/api/Employee/{employee.Id}");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.True(await deleteResponse.Content.ReadFromJsonAsync<bool>());
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Theory]
    [InlineData(EmployeeType.Owner, true)]
    [InlineData(EmployeeType.Manager, true)]
    [InlineData(EmployeeType.FrontDesk, false)]
    public async Task IsAdmin_ReturnsExpectedResult(EmployeeType type, bool expected)
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/Employee/IsAdmin", CreateEmployee(type));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, await response.Content.ReadFromJsonAsync<bool>());
    }

    [Theory]
    [InlineData(EmployeeType.Owner, true)]
    [InlineData(EmployeeType.Manager, false)]
    [InlineData(EmployeeType.FrontDesk, false)]
    public async Task IsOwner_ReturnsExpectedResult(EmployeeType type, bool expected)
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/Employee/IsOwner", CreateEmployee(type));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, await response.Content.ReadFromJsonAsync<bool>());
    }

    private async Task ResetEmployeesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ShiftrDbContext>();
        database.Employees.RemoveRange(await database.Employees.ToListAsync());
        await database.SaveChangesAsync();

        if (!await database.Properties.AnyAsync())
        {
            database.Properties.Add(new PropertyModel { Name = "Test Property" });
            await database.SaveChangesAsync();
        }
    }

    private static async Task<ManagerModel> CreateManagerAsync(HttpClient client)
    {
        var employee = CreateManager();
        using var response = await client.PostAsJsonAsync<EmployeeBase>("/api/Employee/Create", employee);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        employee.Id = document.RootElement.GetProperty("id").GetInt32();
        return employee;
    }

    private static ManagerModel CreateManager() => new()
    {
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
}