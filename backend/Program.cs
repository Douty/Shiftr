using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shiftr.Controllers;
using Shiftr.Data;
using Shiftr.Interface;
using Shiftr.Middleware;
using Shiftr.Models;
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
        policy.RequireRole(IdentityRoles.Employees.ToArray()));
    options.AddPolicy(AuthorizationPolicies.ResidentOnly, policy =>
        policy.RequireRole(IdentityRoles.Resident));
});

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddApplicationPart(typeof(EmployeeController).Assembly);
builder.Services.AddDbContext<ShiftrDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IResidentRepository, ResidentRepository>();
builder.Services.AddScoped<IResidentService, ResidentService>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IAmenityRepository, AmenityRepository>();
builder.Services.AddScoped<IAmenityService, AmenityService>();
builder.Services.AddScoped<IShiftNoteRepository, ShiftNoteRepository>();
builder.Services.AddScoped<IShiftNoteService, ShiftNoteService>();
builder.Services.AddScoped<Shiftr.Services.EmployeeAccessRequestService>();



var app = builder.Build();

var bootstrapOwnerEmail = builder.Configuration["BootstrapOwner:Email"]?.Trim();
var bootstrapOwnerPassword = builder.Configuration["BootstrapOwner:Password"];
var developmentDataPassword = builder.Configuration["DevelopmentData:Password"];
var hasBootstrapOwnerSettings =
    !string.IsNullOrWhiteSpace(bootstrapOwnerEmail) ||
    !string.IsNullOrWhiteSpace(bootstrapOwnerPassword);
var hasDevelopmentDataSettings = !string.IsNullOrWhiteSpace(developmentDataPassword);

if (hasBootstrapOwnerSettings && !app.Environment.IsDevelopment())
{
    throw new InvalidOperationException("Owner bootstrap settings are only supported in Development.");
}

if (hasDevelopmentDataSettings && !app.Environment.IsDevelopment())
{
    throw new InvalidOperationException("Development data seeding is only supported in Development.");
}

if (hasBootstrapOwnerSettings &&
    (string.IsNullOrWhiteSpace(bootstrapOwnerEmail) || string.IsNullOrWhiteSpace(bootstrapOwnerPassword)))
{
    throw new InvalidOperationException("Both BootstrapOwner:Email and BootstrapOwner:Password must be configured.");
}

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

    if (hasBootstrapOwnerSettings)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var ownerProfile = await database.Employees
            .OfType<OwnerModel>()
            .FirstOrDefaultAsync(owner => owner.Email.ToLower() == bootstrapOwnerEmail!.ToLower());
        if (ownerProfile is null)
        {
            throw new InvalidOperationException(
                "Could not find an existing Owner profile for BootstrapOwner:Email.");
        }

        var owner = await userManager.FindByEmailAsync(bootstrapOwnerEmail!);
        if (owner is null)
        {
            owner = new IdentityUser
            {
                UserName = bootstrapOwnerEmail,
                Email = bootstrapOwnerEmail,
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(owner, bootstrapOwnerPassword!);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Could not create development owner account: {errors}");
            }
        }
        else
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(owner);
            var resetResult = await userManager.ResetPasswordAsync(owner, resetToken, bootstrapOwnerPassword!);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join(", ", resetResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Could not reset development owner password: {errors}");
            }
        }

        if (ownerProfile.IdentityUserId is not null && ownerProfile.IdentityUserId != owner.Id)
        {
            throw new InvalidOperationException(
                "The configured Owner profile is already linked to a different sign-in account.");
        }

        if (!await userManager.IsInRoleAsync(owner, IdentityRoles.Owner))
        {
            var roleResult = await userManager.AddToRoleAsync(owner, IdentityRoles.Owner);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Could not assign the development owner role: {errors}");
            }
        }

        ownerProfile.IdentityUserId = owner.Id;
        await database.SaveChangesAsync();
    }

    if (hasDevelopmentDataSettings)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        await DevelopmentDataSeeder.SeedAsync(database, userManager, developmentDataPassword!);
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
app.UseMiddleware<InviteRegistrationValidationMiddleware>();
app.MapIdentityApi<IdentityUser>();
app.MapControllers();

app.Run();
