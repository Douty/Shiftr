using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Shiftr.Security;
using Shiftr.Models;
using Shiftr.Interface;
namespace Shiftr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _service;
        public  EmployeeController(IEmployeeService service)
        {
            _service = service;
        }
        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.RegularEmployee)]
        public async Task<ActionResult<EmployeeBase>> GetEmployee(int id)
        {
            var employee = await _service.GetEmployeeById(id);
            if (employee is null) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.CanAccessEmployee(id, identityUserId)) return Forbid();
            return Ok(employee);
        }

        [HttpGet("HasProfile")]
        [Authorize]
        public async Task<ActionResult<bool>> HasProfile()
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return identityUserId is null
                ? Unauthorized()
                : Ok(await _service.HasEmployeeProfile(identityUserId));
        }

        [HttpPost("Create")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]

        public async Task<ActionResult<EmployeeBase>> CreateEmployee(EmployeeBase Employee)
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.CanCreateEmployee(Employee, identityUserId)) return Forbid();

            var EmployeeCreated = await _service.CreateEmployee(Employee);
            return CreatedAtAction(nameof(GetEmployee), new {id = EmployeeCreated.Id}, EmployeeCreated);
        }
        [HttpDelete("Delete/{id}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<ActionResult<bool>> DeleteEmployee(int Id)
        {
            if (await _service.GetEmployeeById(Id) is null) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.CanAccessEmployee(Id, identityUserId)) return Forbid();

            return await _service.DeleteEmployee(Id);
        }
        [HttpPost("Update")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]

        public async Task<ActionResult<EmployeeBase>> UpdateEmployee(EmployeeBase Employee)
        {
            if (await _service.GetEmployeeById(Employee.Id) is null) return NotFound();
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identityUserId is null || !await _service.CanAccessEmployee(Employee.Id, identityUserId)) return Forbid();

            var EmployeeUpdated = await _service.UpdateEmployee(Employee);
            return EmployeeUpdated is null ? NotFound() : Ok(EmployeeUpdated);
        }
        [HttpGet("IsAdmin")]
        [Authorize]
        public ActionResult<bool> IsAdmin()
        {
            return Ok(User.IsInRole(IdentityRoles.Owner) || User.IsInRole(IdentityRoles.Admin));
        }

        [HttpGet("IsOwner")]
        [Authorize]
        public ActionResult<bool> IsOwner()
        {
            return Ok(User.IsInRole(IdentityRoles.Owner));
        }
        

    }
}
