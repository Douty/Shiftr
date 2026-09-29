using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shiftr.DTOs;
using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = nameof(EmployeeType.Owner))]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationService _service;

        public OrganizationController(IOrganizationService service)
        {
            _service = service;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrganizationModel>> GetOrganization(int id)
        {
            var organization = await _service.GetOrganizationById(id);
            return organization is null ? NotFound() : Ok(organization);
        }

        [HttpPost]
        public async Task<ActionResult<OrganizationModel>> CreateOrganization(OrganizationModel organization)
        {
            var createdOrganization = await _service.CreateOrganization(organization);
            return CreatedAtAction(nameof(GetOrganization), new { id = createdOrganization.Id }, createdOrganization);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<OrganizationModel>> UpdateOrganization(int id, OrganizationModel organization)
        {
            if (id != organization.Id) return BadRequest();

            var updatedOrganization = await _service.UpdateOrganization(organization);
            return updatedOrganization is null ? NotFound() : Ok(updatedOrganization);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            return await _service.DeleteOrganization(id) ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/employees")]
        public async Task<IActionResult> AddEmployee(int id, AddOrganizationEmployeeRequest request)
        {
            var added = await _service.AddEmployeeToOrganization(id, request.EmployeeId, request.PropertyId);
            return added ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/properties")]
        public async Task<ActionResult<PropertyModel>> AddProperty(int id, PropertyModel property)
        {
            var addedProperty = await _service.AddPropertyToOrganization(id, property);
            return addedProperty is null ? NotFound() : Ok(addedProperty);
        }
    }
}