
namespace TrafficFlowOptimizer.Api.Models
{
    public class Vehicle
    {
        public string Id { get; set; } = "";

        public string VehicleNumber { get; set; } = "";

        public string Type { get; set; } = "Car";

        // Side from which the vehicle enters
        public string RoadId { get; set; } = "";

        // Side towards which the vehicle is travelling
        public string DestinationRoadId { get; set; } = "";

        public int LaneNumber { get; set; } = 1;

        // Incoming or Outgoing
        public string LaneDirection { get; set; } = "Incoming";

        public double DistanceToStopLine { get; set; } = 100;

        public double Speed { get; set; } = 5;

        public double WaitingTime { get; set; } = 0;

        public string Status { get; set; } = "Moving";
    }
}
