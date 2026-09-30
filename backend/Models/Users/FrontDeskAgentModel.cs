using System;
using System.Text.Json.Serialization;



namespace Shiftr.Models
{
    public class FrontDeskAgentModel : EmployeeBase
    {
        public override EmployeeType Type => EmployeeType.FrontDesk;
        public required int PropteryId {get; set;}
        [JsonIgnore]
        public PropertyModel? Proptery {get; set;}
        public List<Shift> ShiftWorked { get; set; } = new();
    }
}