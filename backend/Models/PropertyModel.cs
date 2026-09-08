

namespace Shiftr.Models
{
    public class PropertyModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        
        public required List<FrontDeskAgentModel> FrontDeskAgents { get; set; } = new();



    }
}