
using TrafficFlowOptimizer.Api.Models;

namespace TrafficFlowOptimizer.Api.Services
{
    public class SimulationService
    {
        private readonly SimulationState state = new SimulationState();
        private readonly Random random = new Random();
        private readonly object lockObject = new object();

        private int vehicleCounter = 0;

        // Vehicle spacing settings
        private const double VehicleGap = 16;
        private const double StopLineBuffer = 5;

        // Traffic signal settings
        private readonly string[] signalOrder =
        {
            "North", "East", "South", "West"
        };

        private const int SignalGreenDurationTicks = 10;

        private int currentSignalIndex = 0;
        private int signalPhaseTick = 0;

        public SimulationService()
        {
            InitializeRoads();
            ApplyTrafficSignal();
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

        // Apply green signal to only one direction
        private void ApplyTrafficSignal()
        {
            string greenRoad = signalOrder[currentSignalIndex];

            foreach (Road road in state.Roads)
            {
                road.IsGreen = road.Id == greenRoad;
            }
        }

        // Automatically rotate traffic signals
        private void UpdateTrafficSignals()
        {
            signalPhaseTick++;

            if (signalPhaseTick >= SignalGreenDurationTicks)
            {
                signalPhaseTick = 0;

                currentSignalIndex++;

                if (currentSignalIndex >= signalOrder.Length)
                {
                    currentSignalIndex = 0;
                }

                ApplyTrafficSignal();
            }
        }

        // Start simulation
        public void Start()
        {
            lock (lockObject)
            {
                state.IsRunning = true;
            }
        }

        // Pause simulation
        public void Pause()
        {
            lock (lockObject)
            {
                state.IsRunning = false;
            }
        }

        // Reset simulation
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

                currentSignalIndex = 0;
                signalPhaseTick = 0;

                foreach (Road road in state.Roads)
                {
                    foreach (Lane lane in road.Lanes)
                    {
                        lane.Vehicles.Clear();
                    }
                }

                // North starts with green after reset
                ApplyTrafficSignal();
            }
        }

        // Check whether an incoming lane has space
        private bool CanAcceptIncomingVehicle(Lane lane)
        {
            if (lane.Vehicles.Count == 0)
                return true;

            Vehicle lastVehicle = lane.Vehicles.Last();

            return lastVehicle.DistanceToStopLine <=
                   100 - VehicleGap;
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

                if (!CanAcceptIncomingVehicle(lane))
                    throw new InvalidOperationException(
                        "This lane is full. Wait for vehicles to move forward.");

                Vehicle vehicle = CreateVehicle(
                    road.Id,
                    laneNumber,
                    vehicleType);

                lane.Vehicles.Enqueue(vehicle);

                state.TotalGenerated++;

                return vehicle;
            }
        }

        // Create vehicle and assign random destination
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

        // Change traffic signal manually
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

        // Enable or disable automatic vehicle generation
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

                // Update traffic signal cycle
                UpdateTrafficSignals();

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

                // Process incoming vehicles
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

            if (!CanAcceptIncomingVehicle(lane))
                return;

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
                    if (road.IsGreen && newPosition <= 0)
                    {
                        if (TransferVehicle(vehicle))
                        {
                            continue;
                        }

                        newPosition = StopLineBuffer;
                    }
                    else if (!road.IsGreen)
                    {
                        newPosition = Math.Max(
                            StopLineBuffer,
                            newPosition);
                    }
                }
                else
                {
                    newPosition = Math.Max(
                        newPosition,
                        previousPosition + VehicleGap);
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

        // Transfer vehicle to destination outgoing lane
        private bool TransferVehicle(Vehicle vehicle)
        {
            Road? destinationRoad = state.Roads.FirstOrDefault(
                r => r.Id == vehicle.DestinationRoadId);

            if (destinationRoad == null)
            {
                vehicle.Status = "Completed";

                state.CompletedVehicles.Add(vehicle);
                state.TotalCompleted++;

                return true;
            }

            Lane outgoingLane = destinationRoad.Lanes.First(
                l => l.Direction == "Outgoing");

            if (outgoingLane.Vehicles.Count > 0)
            {
                Vehicle lastVehicle = outgoingLane.Vehicles.Last();

                if (lastVehicle.DistanceToStopLine < VehicleGap)
                    return false;
            }

            vehicle.RoadId = destinationRoad.Id;
            vehicle.LaneNumber = outgoingLane.LaneNumber;
            vehicle.LaneDirection = "Outgoing";

            vehicle.DistanceToStopLine = 0;
            vehicle.Status = "Moving";

            outgoingLane.Vehicles.Enqueue(vehicle);

            return true;
        }

        // Move outgoing vehicles away from the intersection
        private void MoveOutgoingVehicles(Lane lane)
        {
            List<Vehicle> vehicles = lane.Vehicles.ToList();

            lane.Vehicles.Clear();

            double previousPosition = double.PositiveInfinity;

            foreach (Vehicle vehicle in vehicles)
            {
                double oldPosition = vehicle.DistanceToStopLine;

                double newPosition = oldPosition + vehicle.Speed;

                newPosition = Math.Max(
                    oldPosition,
                    Math.Min(
                        newPosition,
                        previousPosition - VehicleGap));

                vehicle.DistanceToStopLine = newPosition;

                if (newPosition >= 100)
                {
                    vehicle.Status = "Completed";

                    state.CompletedVehicles.Add(vehicle);
                    state.TotalCompleted++;

                    continue;
                }

                if (newPosition == oldPosition)
                {
                    vehicle.Status = "Waiting";
                    vehicle.WaitingTime++;
                }
                else
                {
                    vehicle.Status = "Moving";
                }

                previousPosition = newPosition;

                lane.Vehicles.Enqueue(vehicle);
            }
        }
    }
}
