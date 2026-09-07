using System;
using Shiftr.Interfaces;


namespace Shiftr.Models
{
    public class FrontDeskAgentModel : EmployeeBase
    {
        public List<Shift> ShiftWorked { get; set; } = new();
    }
}