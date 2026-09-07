namespace Shiftr.Interfaces
{
    public interface IEmployee
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime HireDate { get; set; }

        public EmployeeType type {get; set;}
    }
    
    public enum EmployeeType
    {
        Owner,
        Manager,
        FrontDesk
    }
}