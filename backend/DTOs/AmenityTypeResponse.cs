namespace Shiftr.DTOs
{
    /// <summary>Property amenity type details.</summary>
    public sealed record AmenityTypeResponse(int Id, int PropertyId, string Name, string? Description);
}