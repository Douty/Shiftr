using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<EmployeeBase>> GetEmployee(int id)
        {
            var employee = await _service.GetEmployeeById(id);
            return employee is null ? NotFound() : Ok(employee);
        }
        [HttpPost("Create")]
        public async Task<ActionResult<EmployeeBase>> CreateEmployee(EmployeeBase Employee)
        {
            var EmployeeCreated = await _service.CreateEmployee(Employee);
            return CreatedAtAction(nameof(GetEmployee), new {id = EmployeeCreated.Id}, EmployeeCreated);
        }
        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult<bool>> DeleteEmployee(int Id)
        {
            return await _service.DeleteEmployee(Id);
        }
        [HttpPost("Update")]
        public async Task<ActionResult<EmployeeBase>> UpdateEmployee(EmployeeBase Employee)
        {
            var EmployeeUpdated = await _service.UpdateEmployee(Employee);
            return EmployeeUpdated;
        }
        [HttpPost("IsAdmin")]
        public ActionResult<bool> IsAdmin(EmployeeBase Employee)
        {
            return Ok(_service.IsAdmin(Employee));
        }

        [HttpPost("IsOwner")]
        public ActionResult<bool> IsOwner(EmployeeBase Employee)
        {
            return Ok(_service.IsOwner(Employee));
        }
        

    }
}
