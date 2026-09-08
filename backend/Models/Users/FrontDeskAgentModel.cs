using System;



namespace Shiftr.Models
{
    public class FrontDeskAgentModel : EmployeeBase
    {
        public override EmployeeType Type => EmployeeType.FrontDesk;
        public List<Shift> ShiftWorked { get; set; } = new();
    }
}