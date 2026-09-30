

using System.Text.Json.Serialization;

namespace Shiftr.Models
{
    public class ManagerModel : EmployeeBase
    {
        public  override EmployeeType Type => EmployeeType.Manager;
        public required int PropteryId {get; set;}
        [JsonIgnore]
        public PropertyModel? Proptery {get; set;}
    }
}