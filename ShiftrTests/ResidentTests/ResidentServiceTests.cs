using Shiftr.Interface;
using Shiftr.Models;
using Shiftr.Services;

namespace ShiftrTests;

[Trait("Category", "Unit")]
public class ResidentServiceTests
{
    [Fact]
    public async Task AddAllowedGuestTrimsNameAndIgnoresCaseInsensitiveDuplicates()
    {
        var repository = new FakeResidentRepository();
        var service = new ResidentService(repository);

        await service.AddAllowedGuest(1, 1, "  Morgan  ");
        await service.AddAllowedGuest(1, 1, "morgan");

        Assert.Equal(["Morgan"], repository.Resident.AllowedGuests);
    }

    [Fact]
    public async Task RemoveAllowedGuestMatchesWithoutCaseSensitivity()
    {
        var repository = new FakeResidentRepository();
        repository.Resident.AllowedGuests.Add("Morgan");
        var service = new ResidentService(repository);

        var updated = await service.RemoveAllowedGuest(1, 1, "morgan");

        Assert.NotNull(updated);
        Assert.Empty(updated.AllowedGuests);
    }

    [Fact]
    public async Task SetCallToNotifyPersistsPreference()
    {
        var repository = new FakeResidentRepository();
        var service = new ResidentService(repository);

        var updated = await service.SetCallToNotify(1, 1, true);

        Assert.NotNull(updated);
        Assert.True(updated.CallToNotify);
    }

    private sealed class FakeResidentRepository : IResidentRepository
    {
        public ResidentModel Resident { get; } = new()
        {
            Id = 1,
            PropertyId = 1,
            FirstName = "Taylor",
            LastName = "Resident"
        };

        public Task<IReadOnlyList<ResidentModel>> GetByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ResidentModel>>([Resident]);

        public Task<ResidentModel?> GetByIdAsync(int propertyId, int residentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ResidentModel?>(propertyId == Resident.PropertyId && residentId == Resident.Id ? Resident : null);

        public Task<ResidentModel?> AddAsync(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default) =>
            Task.FromResult<ResidentModel?>(resident);

        public Task<ResidentModel?> UpdateAsync(int propertyId, ResidentModel resident, CancellationToken cancellationToken = default) =>
            Task.FromResult<ResidentModel?>(resident);

        public Task<bool> DeleteAsync(int propertyId, int residentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(propertyId == Resident.PropertyId && residentId == Resident.Id);
    }
}
