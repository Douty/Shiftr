

namespace Shiftr.Models
{
    public class PropertyModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        
        public List<ManagerModel> Managers { get; set; } = new();
        public List<FrontDeskAgentModel> FrontDeskAgents { get; set; } = new();



    }
}