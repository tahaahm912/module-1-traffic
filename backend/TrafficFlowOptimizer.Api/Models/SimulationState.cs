
namespace TrafficFlowOptimizer.Api.Models
{
    public class SimulationState
    {
        public bool IsRunning { get; set; } = false;

        public bool AutoGeneration { get; set; } = true;

        public int TickCount { get; set; } = 0;

        public int TotalGenerated { get; set; } = 0;

        public int TotalCompleted { get; set; } = 0;

        public List<Road> Roads { get; set; } = new List<Road>();

        public List<Vehicle> CompletedVehicles { get; set; } = new List<Vehicle>();
    }
}
