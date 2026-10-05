using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shiftr.Controllers;
using Shiftr.Data;
using Shiftr.DTOs;
using Shiftr.Models;
using Shiftr.Security;
using ShiftrTests.fixture;

namespace ShiftrTests;

[Trait("Category", "Integration")]
public sealed class ResidentSignupIntegrationTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public ResidentSignupIntegrationTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SignupLinksMatchingResidentProfileAndGrantsResidentRole()
    {
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ShiftrDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var email = $"resident-signup-{Guid.NewGuid():N}@example.com";
        var user = new IdentityUser { UserName = email, Email = email };
        var createUserResult = await userManager.CreateAsync(user, "ValidPass123!");
        Assert.True(createUserResult.Succeeded);

        var property = await database.Properties.FirstAsync(item =>
            item.ResidentInviteId == ApiTestFactory.RegistrationInviteCode);
        var existingResident = new ResidentModel
        {
            PropertyId = property.Id,
            FirstName = "Jamie",
            LastName = "Lee",
            UnitNumber = "4B"
        };
        database.Residents.Add(existingResident);
        await database.SaveChangesAsync();

        var controller = new ResidentSignupController(database, userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, user.Id)],
                        "test"))
                }
            }
        };

        var result = await controller.CreateResidentProfile(new CreateResidentSignupRequest
        {
            InviteCode = ApiTestFactory.RegistrationInviteCode,
            FirstName = " jamie ",
            LastName = "LEE",
            UnitNumber = " 4b "
        }, CancellationToken.None);

        Assert.IsType<CreatedResult>(result);
        Assert.Equal(user.Id, existingResident.IdentityUserId);
        Assert.True(await userManager.IsInRoleAsync(user, IdentityRoles.Resident));
        Assert.Equal(1, await database.Residents.CountAsync(item => item.IdentityUserId == user.Id));
    }
}
