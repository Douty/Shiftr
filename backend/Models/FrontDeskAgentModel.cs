using System;


namespace Shiftr.Models
{
    public class FrontDeskAgentModel
    {
        public int Id { get; set;}
        public required string FirstName {get; set;}
        public required string LastName {get; set;}
        public string FullName => $"{FirstName} {LastName}";
        public required string Email {get; set;}
        public required string PhoneNumber {get; set;}
        public DateTime HireDate {get; set;}

        public WorkShifts ShiftWorked {get; set;}

    }
}