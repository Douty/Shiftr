namespace Shiftr.Models;


public class ResidentModel
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public List<string> AllowedGuests { get; set; } = [];
    public bool CallToNotify { get; set; }
    public string? IdentityUserId { get; set; }
    public PropertyModel? Property { get; set; }
}
