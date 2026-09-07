using System;
using Shiftr.Interfaces;


namespace Shiftr.Models
{
    public class FrontDeskAgentModel : EmployeeBase
    {
        public WorkShifts ShiftWorked { get; set; }
    }
}