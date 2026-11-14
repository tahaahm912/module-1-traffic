
namespace TrafficFlowOptimizer.Api.Models
{
    public class Lane
    {
        public int LaneNumber { get; set; } = 1;

        public string Direction { get; set; } = "Incoming";

        public Queue<Vehicle> Vehicles { get; set; } = new Queue<Vehicle>();
    }
}
