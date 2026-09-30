using System;

namespace Shiftr.Models
{
    public enum OrganizationDeleteResult
        {
            Deleted,
            NotFound,
            HasDependents
        }
    public class OrganizationModel
    {
        public int Id {get; set;}
        public required string Name {get; set;}

        public List<OwnerModel> Owners {get; set;} = new();
        public List<PropertyModel> Properties {get; set;} = new();
        
    }
}