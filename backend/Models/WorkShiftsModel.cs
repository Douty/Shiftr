using System;

namespace Shiftr.Models
{
    public enum WorkShifts
    {
        Morning,
        Night,
        Overnight
    }
    public struct Shift
    {
        public WorkShifts Type { get; set; }
        public TimeOnly Start { get; set; }
        public TimeOnly End { get; set; }
    }
}