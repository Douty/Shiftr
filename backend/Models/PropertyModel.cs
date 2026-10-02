

using System.Security.Cryptography;

namespace Shiftr.Models
{
    public enum PropertyInviteType
    {
        Resident,
        Employee
    }

    public enum PropertyDeleteResult
    {
        Deleted,
        NotFound,
        HasEmployees
    }

    public class PropertyModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string ResidentInviteId { get; set; } = GenerateInviteId();
        public string EmployeeInviteId { get; set; } = GenerateInviteId();
        
        public List<ManagerModel> Managers { get; set; } = new();
        public List<FrontDeskAgentModel> FrontDeskAgents { get; set; } = new();

        public string RotateInviteId(PropertyInviteType inviteType)
        {
            var inviteId = GenerateInviteId();
            switch (inviteType)
            {
                case PropertyInviteType.Resident:
                    ResidentInviteId = inviteId;
                    break;
                case PropertyInviteType.Employee:
                    EmployeeInviteId = inviteId;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inviteType));
            }

            return inviteId;
        }

        private static string GenerateInviteId() =>
            Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    }
}