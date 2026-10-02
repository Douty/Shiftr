using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiftr.Controllers;
using Shiftr.Data;
using Shiftr.Models;
using Testcontainers.PostgreSql;

namespace ShiftrTests.fixture
{
public sealed class ApiTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string RegistrationInviteCode = "TEST-RESIDENT-INVITE";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ShiftrDbContext>();
            services.RemoveAll<DbContextOptions<ShiftrDbContext>>();

            services.AddDbContext<ShiftrDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShiftrDbContext>();
        await db.Database.MigrateAsync();
        db.Properties.Add(new PropertyModel
        {
            Name = "Registration Test Property",
            ResidentInviteId = RegistrationInviteCode,
            EmployeeInviteId = "TEST-EMPLOYEE-INVITE"
        });
        await db.SaveChangesAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
    
    
}