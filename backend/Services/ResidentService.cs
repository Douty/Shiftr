using Shiftr.Interface;
using Shiftr.Models;

namespace Shiftr.Services
{
    public class ResidentService : IResidentService
    {
        private readonly IResidentRepository _repository;

        public ResidentService(IResidentRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyList<ResidentModel>> GetResidents(int propertyId, CancellationToken cancellationToken = default) =>
            _repository.GetByPropertyIdAsync(propertyId, cancellationToken);

        public Task<ResidentModel?> GetResident(int propertyId, int residentId, CancellationToken cancellationToken = default) =>
            _repository.GetByIdAsync(propertyId, residentId, cancellationToken);

        public Task<ResidentModel?> CreateResident(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default) =>
            _repository.AddAsync(propertyId, resident, cancellationToken);

        public Task<ResidentModel?> UpdateResident(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default) =>
            _repository.UpdateAsync(propertyId, resident, cancellationToken);

        public Task<bool> DeleteResident(int propertyId, int residentId, CancellationToken cancellationToken = default) =>
            _repository.DeleteAsync(propertyId, residentId, cancellationToken);
    }
}