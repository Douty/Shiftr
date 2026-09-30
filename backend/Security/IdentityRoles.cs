namespace Shiftr.Security
{
    public static class IdentityRoles
    {
        public const string Owner = "Owner";
        public const string Admin = "Admin";
        public const string FrontDesk = "Front Desk";

        public static IReadOnlyList<string> All { get; } = [Owner, Admin, FrontDesk];
    }
}