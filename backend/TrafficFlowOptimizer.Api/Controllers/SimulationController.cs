
using Microsoft.AspNetCore.Mvc;
using TrafficFlowOptimizer.Api.Services;

namespace TrafficFlowOptimizer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimulationController : ControllerBase
    {
        private readonly SimulationService simulation;

        public SimulationController(SimulationService simulation)
        {
            this.simulation = simulation;
        }

        // Get complete simulation state
        [HttpGet]
        public IActionResult GetState()
        {
            return Ok(simulation.GetState());
        }

        // Start simulation
        [HttpPost("start")]
        public IActionResult Start()
        {
            simulation.Start();

            return Ok(new
            {
                message = "Simulation started"
            });
        }

        // Pause simulation
        [HttpPost("pause")]
        public IActionResult Pause()
        {
            simulation.Pause();

            return Ok(new
            {
                message = "Simulation paused"
            });
        }

        // Reset simulation
        [HttpPost("reset")]
        public IActionResult Reset()
        {
            simulation.Reset();

            return Ok(new
            {
                message = "Simulation reset"
            });
        }

        // Run one simulation tick
        [HttpPost("tick")]
        public IActionResult Tick()
        {
            simulation.Tick();

            return Ok(simulation.GetState());
        }

        // Add a vehicle manually
        [HttpPost("vehicles")]
        public IActionResult AddVehicle([FromBody] AddVehicleRequest request)
        {
            try
            {
                var vehicle = simulation.AddVehicle(
                    request.RoadId,
                    request.LaneNumber,
                    request.VehicleType
                );

                return Ok(vehicle);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // Change a road's traffic signal
        [HttpPut("roads/{roadId}/signal")]
        public IActionResult SetSignal(
            string roadId,
            [FromBody] SignalRequest request)
        {
            try
            {
                simulation.SetSignal(roadId, request.IsGreen);

                return Ok(new
                {
                    message = "Signal updated"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // Enable or disable automatic vehicle generation
        [HttpPut("auto-generation")]
        public IActionResult SetAutoGeneration(
            [FromBody] AutoGenerationRequest request)
        {
            simulation.SetAutoGeneration(request.Enabled);

            return Ok(new
            {
                message = "Automatic generation updated"
            });
        }
    }

    public class AddVehicleRequest
    {
        public string RoadId { get; set; } = "";

        public int LaneNumber { get; set; }

        public string VehicleType { get; set; } = "Car";
    }

    public class SignalRequest
    {
        public bool IsGreen { get; set; }
    }

    public class AutoGenerationRequest
    {
        public bool Enabled { get; set; }
    }
}
