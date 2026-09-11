using System;

namespace Shiftr.Models
{
    public class OrganizationModel
    {
        public int Id {get; set;}
        public required string Name;

        public List<OwnerModel> Owners {get; set;} = new();
        public required List<PropertyModel> Properties = new();
        
    }
}