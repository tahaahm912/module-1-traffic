
namespace TrafficFlowOptimizer.Api.Models
{
    public class Road
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public bool IsGreen { get; set; } = false;

        public List<Lane> Lanes { get; set; } = new List<Lane>();
    }
}
