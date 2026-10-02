using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shiftr.Data;
using Shiftr.Models;
using Shiftr.Security;
using ShiftrTests.fixture;

namespace ShiftrTests;

[Trait("Category", "Integration")]
public sealed class EmployeeControllerIntegrationTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public EmployeeControllerIntegrationTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    private static async Task SetBearerTokenAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/login?useCookies=false",
            new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task AddRoleAsync(string email, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        var result = await userManager.AddToRoleAsync(user!, role);
        Assert.True(result.Succeeded);
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

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Secure-Pass123!";

        using var registerClient = _factory.CreateClient();
        using var registerResponse = await registerClient.PostAsJsonAsync("/register", new
        {
            email,
            password,
            inviteCode = ApiTestFactory.RegistrationInviteCode
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        await AddRoleAsync(email, IdentityRoles.Admin);

        int propertyId;
        int authenticatedOrganizationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);

            var database = services.GetRequiredService<ShiftrDbContext>();
            var organization = new OrganizationModel { Name = $"Test Org {Guid.NewGuid():N}" };
            var property = new PropertyModel { Name = "Test Property" };
            organization.Properties.Add(property);
            database.Organizations.Add(organization);
            await database.SaveChangesAsync();

            database.Employees.Add(new ManagerModel
            {
                FirstName = "Test",
                LastName = "Admin",
                Email = email,
                PhoneNumber = "555-0100",
                PropteryId = property.Id,
                IdentityUserId = user!.Id
            });
            await database.SaveChangesAsync();
            propertyId = property.Id;
            authenticatedOrganizationId = organization.Id;
        }

        var authenticatedClient = _factory.CreateClient();
        await SetBearerTokenAsync(authenticatedClient, email, password);
        authenticatedClient.DefaultRequestHeaders.Add("X-Test-Property-Id", propertyId.ToString());
        authenticatedClient.DefaultRequestHeaders.Add("X-Test-Organization-Id", authenticatedOrganizationId.ToString());
        return authenticatedClient;
    }

    private async Task<HttpClient> CreateRoleClientAsync(string role)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Secure-Pass123!";
        var client = _factory.CreateClient();
        using var registerResponse = await client.PostAsJsonAsync("/register", new
        {
            email,
            password,
            inviteCode = ApiTestFactory.RegistrationInviteCode
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        await AddRoleAsync(email, role);
        await SetBearerTokenAsync(client, email, password);
        return client;
    }

    private static async Task<ManagerModel> CreateManagerAsync(HttpClient client)
    {
        var employee = CreateManager(client);
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

    private static ManagerModel CreateManager(HttpClient client) => new()
    {
        FirstName = "Taylor",
        LastName = "Reed",
        Email = "taylor@example.com",
        PhoneNumber = "555-0100",
        PropteryId = int.Parse(client.DefaultRequestHeaders.GetValues("X-Test-Property-Id").Single())
    };

    [Fact]
    public async Task CreateEmployee_ReturnsCreatedAndEmployeeCanBeRetrieved()
    {
        await ResetEmployeesAsync();
        using var client = await CreateAdminClientAsync();
        using var createResponse = await client.PostAsJsonAsync<EmployeeBase>(
            "/api/Employee/Create", CreateManager(client));

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
    public async Task DeleteEmployee_RemovesEmployee()
    {
        await ResetEmployeesAsync();
        using var client = await CreateAdminClientAsync();
        var employee = await CreateManagerAsync(client);

        using var deleteResponse = await client.DeleteAsync($"/api/Employee/Delete/{employee.Id}");
        using var getResponse = await client.GetAsync($"/api/Employee/{employee.Id}");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.True(await deleteResponse.Content.ReadFromJsonAsync<bool>());
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetEmployee_RequiresAuthorization()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/Employee/999999");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminCannotAccessAnotherOrganizationsData()
    {
        using var firstAdmin = await CreateAdminClientAsync();
        using var secondAdmin = await CreateAdminClientAsync();
        var employee = await CreateManagerAsync(firstAdmin);
        var organizationId = firstAdmin.DefaultRequestHeaders
            .GetValues("X-Test-Organization-Id").Single();

        using var employeeReadResponse = await secondAdmin.GetAsync($"/api/Employee/{employee.Id}");
        using var readResponse = await secondAdmin.GetAsync($"/api/Organization/{organizationId}");
        using var addPropertyResponse = await secondAdmin.PostAsJsonAsync(
            $"/api/Organization/{organizationId}/properties",
            new PropertyModel { Name = "Unauthorized Property" });

    Assert.Equal(HttpStatusCode.Forbidden, employeeReadResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, addPropertyResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsNotFoundWhenEmployeeDoesNotExist()
    {
        using var client = await CreateAdminClientAsync();

        using var response = await client.PostAsJsonAsync<EmployeeBase>(
            "/api/Employee/Update", CreateManager(client));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_PersistsUpdatedEmployee()
    {
        await ResetEmployeesAsync();
        using var client = await CreateAdminClientAsync();
        var employee = await CreateManagerAsync(client);
        employee.LastName = "Morgan";

        using var updateResponse = await client.PostAsJsonAsync<EmployeeBase>("/api/Employee/Update", employee);
        using var getResponse = await client.GetAsync($"/api/Employee/{employee.Id}");

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        using var employeeDocument = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal("Morgan", employeeDocument.RootElement.GetProperty("lastName").GetString());
    }

    [Theory]
    [InlineData(EmployeeType.Owner, true)]
    [InlineData(EmployeeType.Manager, true)]
    [InlineData(EmployeeType.FrontDesk, false)]
    public async Task IsAdmin_ReturnsExpectedResult(EmployeeType type, bool expected)
    {
        var role = type switch
        {
            EmployeeType.Owner => IdentityRoles.Owner,
            EmployeeType.Manager => IdentityRoles.Admin,
            _ => IdentityRoles.FrontDesk
        };
        using var client = await CreateRoleClientAsync(role);

        using var response = await client.GetAsync("/api/Employee/IsAdmin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, await response.Content.ReadFromJsonAsync<bool>());
    }

    [Theory]
    [InlineData(EmployeeType.Owner, true)]
    [InlineData(EmployeeType.Manager, false)]
    [InlineData(EmployeeType.FrontDesk, false)]
    public async Task IsOwner_ReturnsExpectedResult(EmployeeType type, bool expected)
    {
        var role = type == EmployeeType.Owner ? IdentityRoles.Owner : IdentityRoles.Admin;
        using var client = await CreateRoleClientAsync(role);

        using var response = await client.GetAsync("/api/Employee/IsOwner");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, await response.Content.ReadFromJsonAsync<bool>());
    }

    [Fact]
    public async Task IdentityApi_CanRegisterAndLogin()
    {
        using var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Secure-Pass123!";

        using var registerResponse = await client.PostAsJsonAsync("/register", new
        {
            email,
            password,
            inviteCode = ApiTestFactory.RegistrationInviteCode
        });
        using var loginResponse = await client.PostAsJsonAsync(
            "/login?useCookies=false",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginDocument = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(loginDocument.RootElement.GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task Registration_RejectsInvalidInviteCodeBeforeCreatingUser()
    {
        using var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Secure-Pass123!";

        using var response = await client.PostAsJsonAsync("/register", new
        {
            email,
            password,
            inviteCode = "NOT-A-VALID-INVITE"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.Null(await userManager.FindByEmailAsync(email));
    }

    [Fact]
    public async Task IdentityRoles_AreSeeded()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in IdentityRoles.All)
        {
            Assert.True(await roleManager.RoleExistsAsync(role));
        }
    }

    [Fact]
    public async Task OrganizationEndpoints_ApplyRolePolicies()
    {
        using var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Secure-Pass123!";

        using var registerResponse = await client.PostAsJsonAsync("/register", new
        {
            email,
            password,
            inviteCode = ApiTestFactory.RegistrationInviteCode
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        await SetBearerTokenAsync(client, email, password);
        using var noRoleRead = await client.GetAsync("/api/Organization/999999");
        using var noRoleCreate = await client.PostAsJsonAsync(
            "/api/Organization",
            new OrganizationModel { Name = "No Role Org" });
        Assert.Equal(HttpStatusCode.Forbidden, noRoleRead.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, noRoleCreate.StatusCode);

        await AddRoleAsync(email, IdentityRoles.FrontDesk);
        await SetBearerTokenAsync(client, email, password);
        using var employeeRead = await client.GetAsync("/api/Organization/999999");
        using var employeeAddProperty = await client.PostAsJsonAsync(
            "/api/Organization/999999/properties",
            new PropertyModel { Name = "Front Desk Property" });
        Assert.Equal(HttpStatusCode.NotFound, employeeRead.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, employeeAddProperty.StatusCode);

        await AddRoleAsync(email, IdentityRoles.Admin);
        await SetBearerTokenAsync(client, email, password);
        using var adminAddProperty = await client.PostAsJsonAsync(
            "/api/Organization/999999/properties",
            new PropertyModel { Name = "Admin Property" });
        using var adminCreate = await client.PostAsJsonAsync(
            "/api/Organization",
            new OrganizationModel { Name = "Admin Org" });
        Assert.Equal(HttpStatusCode.NotFound, adminAddProperty.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, adminCreate.StatusCode);

        await AddRoleAsync(email, IdentityRoles.Owner);
        await SetBearerTokenAsync(client, email, password);
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var ownerUser = await userManager.FindByEmailAsync(email);
        Assert.NotNull(ownerUser);
        using var ownerCreate = await client.PostAsJsonAsync(
            "/api/Organization",
            new OrganizationModel
            {
                Name = "Owner Org",
                Owners =
                [
                    new OwnerModel
                    {
                        FirstName = "Test",
                        LastName = "Owner",
                        Email = email,
                        PhoneNumber = "555-0101",
                        OrganizationID = 0,
                        IdentityUserId = ownerUser!.Id
                    }
                ]
            });
        Assert.Equal(HttpStatusCode.Created, ownerCreate.StatusCode);
    }
}
