namespace Shiftr.Security
{
    public static class AuthorizationPolicies
    {
        public const string OwnerOnly = "OwnerOnly";
        public const string AdminOnly = "AdminOnly";
        public const string RegularEmployee = "RegularEmployee";
        public const string ResidentOnly = "ResidentOnly";
    }
}