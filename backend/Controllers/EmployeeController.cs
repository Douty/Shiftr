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
        [HttpPost("create")]
        public async Task<ActionResult<EmployeeBase>> CreateEmployee(EmployeeBase Employee)
        {
            var EmployeeCreated = await _service.CreateEmployee(Employee);
            return CreatedAtAction(nameof(GetEmployee), new {id = EmployeeCreated.Id}, EmployeeCreated);
        }
        
    }
}