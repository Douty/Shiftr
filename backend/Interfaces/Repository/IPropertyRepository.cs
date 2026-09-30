using Shiftr.Models;

namespace Shiftr.Interface
{
    public interface IPropertyRepository
    {
        Task<PropertyModel?> GetByIdAsync(int id);
        Task<PropertyModel?> AddAsync(int organizationId, PropertyModel property);
        Task<PropertyModel?> UpdateAsync(PropertyModel property);
        Task<PropertyDeleteResult> DeleteAsync(int id);
    }
}