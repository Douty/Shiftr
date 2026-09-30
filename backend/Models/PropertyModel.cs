

namespace Shiftr.Models
{
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
        
        public List<ManagerModel> Managers { get; set; } = new();
        public List<FrontDeskAgentModel> FrontDeskAgents { get; set; } = new();



    }
}