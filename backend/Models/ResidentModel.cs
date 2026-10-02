namespace Shiftr.Models;


public class ResidentModel
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? IdentityUserId { get; set; }
    public PropertyModel? Property { get; set; }
}
