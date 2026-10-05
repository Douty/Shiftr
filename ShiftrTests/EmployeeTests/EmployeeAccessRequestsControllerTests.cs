using Microsoft.AspNetCore.Authorization;
using Shiftr.Controllers;
using Shiftr.Security;

namespace ShiftrTests;

[Trait("Category", "Unit")]
public class EmployeeAccessRequestsControllerTests
{
    [Theory]
    [InlineData(nameof(EmployeeAccessRequestsController.GetPendingRequests), AuthorizationPolicies.AdminOnly)]
    [InlineData(nameof(EmployeeAccessRequestsController.Approve), AuthorizationPolicies.AdminOnly)]
    [InlineData(nameof(EmployeeAccessRequestsController.Reject), AuthorizationPolicies.AdminOnly)]
    public void ManagementActions_RequireAdminPolicy(string actionName, string expectedPolicy)
    {
        var authorization = typeof(EmployeeAccessRequestsController)
            .GetMethod(actionName)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(expectedPolicy, authorization.Policy);
    }

    [Theory]
    [InlineData(nameof(EmployeeAccessRequestsController.GetMyPendingRequest))]
    [InlineData(nameof(EmployeeAccessRequestsController.CreateRequest))]
    public void RequesterActions_RequireAuthentication(string actionName)
    {
        var authorization = typeof(EmployeeAccessRequestsController)
            .GetMethod(actionName)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Null(authorization.Policy);
    }
}