
using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Repository;

namespace Shiftr.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _Repository;
        private readonly IOrganizationRepository _organizationRepository;

        public EmployeeService(
            IEmployeeRepository employeeRepository,
            IOrganizationRepository organizationRepository)
        {
            _Repository = employeeRepository;
            _organizationRepository = organizationRepository;
        }
        public async Task<EmployeeBase?> GetEmployeeById(int Id)
        {
            return await _Repository.GetByIdAsync(Id);
        }
        
        public async Task<EmployeeBase> CreateEmployee(EmployeeBase Employee)
        {
            return await _Repository.AddAsync(Employee);
        }

        public async Task<bool> DeleteEmployee(int Id)
        {
            return await _Repository.DeleteAsync(Id);
        }

        public async Task<bool> CanAccessEmployee(int employeeId, string identityUserId)
        {
            if (await _Repository.IsLinkedToIdentityAsync(employeeId, identityUserId)) return true;

            var organizationId = await _organizationRepository.GetOrganizationIdForEmployeeAsync(employeeId);
            return organizationId.HasValue &&
                await _organizationRepository.HasAdminAccessAsync(organizationId.Value, identityUserId);
        }

        public async Task<bool> CanCreateEmployee(EmployeeBase employee, string identityUserId)
        {
            int? organizationId = employee switch
            {
                OwnerModel owner => owner.OrganizationID,
                ManagerModel manager => await _organizationRepository
                    .GetOrganizationIdForPropertyAsync(manager.PropteryId),
                FrontDeskAgentModel agent => await _organizationRepository
                    .GetOrganizationIdForPropertyAsync(agent.PropteryId),
                _ => null
            };

            return organizationId.HasValue &&
                await _organizationRepository.HasAdminAccessAsync(organizationId.Value, identityUserId);
        }

        public async Task<EmployeeBase?> UpdateEmployee(EmployeeBase Employee)
        {
            return await _Repository.UpdateAsync(Employee);
        }
      
        public bool IsAdmin(EmployeeBase Employee)
        {
            return Employee.Type == EmployeeType.Manager || Employee.Type == EmployeeType.Owner;
        }
        public bool IsOwner(EmployeeBase Employee)
        {
            return Employee.Type == EmployeeType.Owner;
        }
      
    }
}