namespace Shiftr.Security
{
    public static class IdentityRoles
    {
        public const string Owner = "Owner";
        public const string Admin = "Admin";
        public const string FrontDesk = "Front Desk";
        public const string Resident = "Resident";

        public static IReadOnlyList<string> Employees { get; } = [Owner, Admin, FrontDesk];
        public static IReadOnlyList<string> All { get; } = [Owner, Admin, FrontDesk, Resident];
    }
}