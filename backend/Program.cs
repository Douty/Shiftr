using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shiftr.Controllers;
using Shiftr.Data;
using Shiftr.Interface;
using Shiftr.Repository;
using Shiftr.Security;
using Shiftr.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ShiftrDbContext>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.OwnerOnly, policy =>
        policy.RequireRole(IdentityRoles.Owner));
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(IdentityRoles.Owner, IdentityRoles.Admin));
    options.AddPolicy(AuthorizationPolicies.RegularEmployee, policy =>
        policy.RequireRole(IdentityRoles.All.ToArray()));
});

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddApplicationPart(typeof(EmployeeController).Assembly);
builder.Services.AddDbContext<ShiftrDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IPropertyService, PropertyService>();



var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ShiftrDbContext>();
    await database.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in IdentityRoles.All)
    {
        if (await roleManager.RoleExistsAsync(roleName)) continue;

        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not create role '{roleName}': {errors}");
        }
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapIdentityApi<IdentityUser>();
app.MapControllers();

app.Run();


