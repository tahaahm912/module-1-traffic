
using TrafficFlowOptimizer.Api.Models;

namespace TrafficFlowOptimizer.Api.Services
{
    public class SimulationService
    {
        private readonly SimulationState state = new SimulationState();
        private readonly Random random = new Random();
        private readonly object lockObject = new object();

        private int vehicleCounter = 0;

        public SimulationService()
        {
            InitializeRoads();
        }

        // Create four sides with incoming and outgoing lanes
        private void InitializeRoads()
        {
            string[] sides = { "North", "South", "East", "West" };

            foreach (string side in sides)
            {
                Road road = new Road
                {
                    Id = side,
                    Name = side,
                    IsGreen = false
                };

                road.Lanes.Add(new Lane
                {
                    LaneNumber = 1,
                    Direction = "Incoming"
                });

                road.Lanes.Add(new Lane
                {
                    LaneNumber = 2,
                    Direction = "Outgoing"
                });

                state.Roads.Add(road);
            }
        }

        public SimulationState GetState()
        {
            lock (lockObject)
            {
                return state;
            }
        }

        public void Start()
        {
            lock (lockObject)
            {
                state.IsRunning = true;
            }
        }

        public void Pause()
        {
            lock (lockObject)
            {
                state.IsRunning = false;
            }
        }

        public void Reset()
        {
            lock (lockObject)
            {
                state.IsRunning = false;
                state.TickCount = 0;
                state.TotalGenerated = 0;
                state.TotalCompleted = 0;

                state.CompletedVehicles.Clear();

                vehicleCounter = 0;

                foreach (Road road in state.Roads)
                {
                    road.IsGreen = false;

                    foreach (Lane lane in road.Lanes)
                    {
                        lane.Vehicles.Clear();
                    }
                }
            }
        }

        // Manually add a vehicle
        public Vehicle AddVehicle(
            string roadId,
            int laneNumber,
            string vehicleType)
        {
            lock (lockObject)
            {
                Road? road = state.Roads.FirstOrDefault(r =>
                    r.Id.Equals(
                        roadId,
                        StringComparison.OrdinalIgnoreCase));

                if (road == null)
                    throw new ArgumentException("Invalid road.");

                Lane? lane = road.Lanes.FirstOrDefault(l =>
                    l.LaneNumber == laneNumber &&
                    l.Direction == "Incoming");

                if (lane == null)
                    throw new ArgumentException(
                        "Vehicle can only enter an incoming lane.");

                string[] allowedTypes =
                {
                    "Car", "Bike", "Bus", "Truck"
                };

                if (!allowedTypes.Contains(vehicleType))
                    throw new ArgumentException("Invalid vehicle type.");

                Vehicle vehicle = CreateVehicle(
                    road.Id,
                    laneNumber,
                    vehicleType);

                lane.Vehicles.Enqueue(vehicle);

                state.TotalGenerated++;

                return vehicle;
            }
        }

        // Create a vehicle and assign a random destination
        private Vehicle CreateVehicle(
            string roadId,
            int laneNumber,
            string vehicleType)
        {
            vehicleCounter++;

            List<Road> possibleDestinations = state.Roads
                .Where(r => r.Id != roadId)
                .ToList();

            Road destination = possibleDestinations[
                random.Next(possibleDestinations.Count)];

            return new Vehicle
            {
                Id = "V" + vehicleCounter.ToString("D4"),
                VehicleNumber = "SIM-" + vehicleCounter.ToString("D4"),

                Type = vehicleType,
                RoadId = roadId,
                DestinationRoadId = destination.Id,

                LaneNumber = laneNumber,
                LaneDirection = "Incoming",

                DistanceToStopLine = 100,

                Speed = vehicleType == "Bus" ||
                        vehicleType == "Truck" ? 3 : 5,

                WaitingTime = 0,
                Status = "Moving"
            };
        }

        // Change traffic signal
        public void SetSignal(string roadId, bool isGreen)
        {
            lock (lockObject)
            {
                Road? selectedRoad = state.Roads.FirstOrDefault(r =>
                    r.Id.Equals(
                        roadId,
                        StringComparison.OrdinalIgnoreCase));

                if (selectedRoad == null)
                    throw new ArgumentException("Invalid road.");

                if (isGreen)
                {
                    foreach (Road road in state.Roads)
                    {
                        road.IsGreen = false;
                    }
                }

                selectedRoad.IsGreen = isGreen;
            }
        }

        // Enable or disable automatic generation
        public void SetAutoGeneration(bool enabled)
        {
            lock (lockObject)
            {
                state.AutoGeneration = enabled;
            }
        }

        // Execute one simulation tick
        public void Tick()
        {
            lock (lockObject)
            {
                if (!state.IsRunning)
                    return;

                state.TickCount++;

                if (state.AutoGeneration &&
                    random.NextDouble() < 0.5)
                {
                    GenerateRandomVehicle();
                }

                // Move outgoing vehicles first
                foreach (Road road in state.Roads)
                {
                    Lane outgoingLane = road.Lanes.First(
                        l => l.Direction == "Outgoing");

                    MoveOutgoingVehicles(outgoingLane);
                }

                // Then process incoming vehicles
                foreach (Road road in state.Roads)
                {
                    Lane incomingLane = road.Lanes.First(
                        l => l.Direction == "Incoming");

                    MoveIncomingVehicles(road, incomingLane);
                }
            }
        }

        // Automatically generate a vehicle
        private void GenerateRandomVehicle()
        {
            Road road = state.Roads[
                random.Next(state.Roads.Count)];

            Lane lane = road.Lanes.First(
                l => l.Direction == "Incoming");

            string[] types =
            {
                "Car", "Car", "Car", "Bike", "Bus", "Truck"
            };

            string vehicleType = types[
                random.Next(types.Length)];

            Vehicle vehicle = CreateVehicle(
                road.Id,
                lane.LaneNumber,
                vehicleType);

            lane.Vehicles.Enqueue(vehicle);

            state.TotalGenerated++;
        }

        // Move incoming vehicles towards the intersection
        private void MoveIncomingVehicles(Road road, Lane lane)
        {
            List<Vehicle> vehicles = lane.Vehicles.ToList();

            lane.Vehicles.Clear();

            double previousPosition = -1;

            foreach (Vehicle vehicle in vehicles)
            {
                double oldPosition = vehicle.DistanceToStopLine;

                double newPosition = oldPosition - vehicle.Speed;

                if (previousPosition < 0)
                {
                    // Vehicle can cross only on green
                    if (road.IsGreen && newPosition <= 0)
                    {
                        TransferVehicle(vehicle);
                        continue;
                    }

                    // Stop at red signal
                    if (!road.IsGreen)
                    {
                        newPosition = Math.Max(5, newPosition);
                    }
                }
                else
                {
                    // Maintain distance between vehicles
                    newPosition = Math.Max(
                        newPosition,
                        previousPosition + 8);
                }

                vehicle.DistanceToStopLine = newPosition;

                if (newPosition >= oldPosition)
                {
                    vehicle.WaitingTime++;
                    vehicle.Status = "Waiting";
                }
                else
                {
                    vehicle.Status = "Moving";
                }

                previousPosition = newPosition;

                lane.Vehicles.Enqueue(vehicle);
            }
        }

        // Transfer a vehicle to its destination outgoing lane
        private void TransferVehicle(Vehicle vehicle)
        {
            Road? destinationRoad = state.Roads.FirstOrDefault(
                r => r.Id == vehicle.DestinationRoadId);

            if (destinationRoad == null)
            {
                vehicle.Status = "Completed";
                state.CompletedVehicles.Add(vehicle);
                state.TotalCompleted++;
                return;
            }

            Lane outgoingLane = destinationRoad.Lanes.First(
                l => l.Direction == "Outgoing");

            vehicle.RoadId = destinationRoad.Id;
            vehicle.LaneNumber = outgoingLane.LaneNumber;
            vehicle.LaneDirection = "Outgoing";

            vehicle.DistanceToStopLine = 0;
            vehicle.Status = "Moving";

            outgoingLane.Vehicles.Enqueue(vehicle);
        }

        // Move outgoing vehicles away from the intersection
        private void MoveOutgoingVehicles(Lane lane)
        {
            int count = lane.Vehicles.Count;

            for (int i = 0; i < count; i++)
            {
                Vehicle vehicle = lane.Vehicles.Dequeue();

                vehicle.DistanceToStopLine += vehicle.Speed;

                if (vehicle.DistanceToStopLine >= 100)
                {
                    vehicle.Status = "Completed";

                    state.CompletedVehicles.Add(vehicle);

                    state.TotalCompleted++;
                }
                else
                {
                    vehicle.Status = "Moving";

                    lane.Vehicles.Enqueue(vehicle);
                }
            }
        }
    }
}
