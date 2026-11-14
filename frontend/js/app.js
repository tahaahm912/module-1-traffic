
/* ==========================================
   TRAFFIC FLOW OPTIMIZER
   FRONTEND JAVASCRIPT
========================================== */

const API = "http://localhost:5115/api/simulation";

let simulationState = null;
let updating = false;

/* ==========================================
   API REQUEST FUNCTION
========================================== */

async function apiRequest(endpoint, method = "GET", body = null) {
    const options = {
        method: method,
        headers: {
            "Content-Type": "application/json"
        }
    };

    if (body !== null) {
        options.body = JSON.stringify(body);
    }

    const response = await fetch(`${API}${endpoint}`, options);

    if (!response.ok) {
        let message = "Request failed";

        try {
            const error = await response.json();
            message = error.message || message;
        } catch {
            message = response.statusText || message;
        }

        throw new Error(message);
    }

    return response.json();
}

/* ==========================================
   CONNECTION STATUS
========================================== */

function setConnectionStatus(connected) {
    const dot = document.getElementById("connectionDot");
    const text = document.getElementById("connectionText");

    if (connected) {
        dot.style.background = "#20b56b";
        text.textContent = "Backend Connected";
    } else {
        dot.style.background = "#e34848";
        text.textContent = "Backend Disconnected";
    }
}

/* ==========================================
   LOAD SIMULATION STATE
========================================== */

async function loadState() {
    try {
        simulationState = await apiRequest("");
        setConnectionStatus(true);

        renderDashboard();
    } catch (error) {
        setConnectionStatus(false);
        console.error("Unable to load simulation:", error);
    }
}

/* ==========================================
   SIMULATION CONTROLS
========================================== */

async function startSimulation() {
    try {
        await apiRequest("/start", "POST");
        await loadState();
    } catch (error) {
        alert(error.message);
    }
}

async function pauseSimulation() {
    try {
        await apiRequest("/pause", "POST");
        await loadState();
    } catch (error) {
        alert(error.message);
    }
}

async function resetSimulation() {
    try {
        await apiRequest("/reset", "POST");
        await loadState();
    } catch (error) {
        alert(error.message);
    }
}

/* ==========================================
   AUTOMATIC VEHICLE GENERATION
========================================== */

async function changeAutoGeneration() {
    const enabled = document.getElementById("autoGeneration").checked;

    try {
        await apiRequest(
            "/auto-generation",
            "PUT",
            { enabled: enabled }
        );

        await loadState();
    } catch (error) {
        alert(error.message);
    }
}

/* ==========================================
   ADD MANUAL VEHICLE
========================================== */

async function addVehicle() {
    const roadId = document.getElementById("roadSelect").value;
    const vehicleType = document.getElementById("vehicleType").value;

    try {
        await apiRequest(
            "/vehicles",
            "POST",
            {
                roadId: roadId,
                laneNumber: 1,
                vehicleType: vehicleType
            }
        );

        await loadState();
    } catch (error) {
        alert(error.message);
    }
}

/* ==========================================
   SIMULATION TICK
========================================== */

async function simulationTick() {
    if (updating) return;

    updating = true;

    try {
        if (simulationState && simulationState.isRunning) {
            await apiRequest("/tick", "POST");
        }

        await loadState();
    } catch (error) {
        console.error("Simulation update failed:", error);
        setConnectionStatus(false);
    } finally {
        updating = false;
    }
}

/* ==========================================
   DASHBOARD RENDERING
========================================== */

function renderDashboard() {
    if (!simulationState) return;

    document.getElementById("tickCount").textContent =
        simulationState.tickCount;

    document.getElementById("totalGenerated").textContent =
        simulationState.totalGenerated;

    document.getElementById("totalCompleted").textContent =
        simulationState.totalCompleted;

    let totalVehicles = 0;

    simulationState.roads.forEach(road => {
        road.lanes.forEach(lane => {
            totalVehicles += lane.vehicles.length;
        });
    });

    document.getElementById("vehiclesInQueue").textContent =
        totalVehicles;

    document.getElementById("simulationStatus").textContent =
        simulationState.isRunning ? "Running" : "Stopped";

    document.getElementById("autoGeneration").checked =
        simulationState.autoGeneration;

    renderSignals();
    renderRoads();
    renderVehicles();
    renderCompletedVehicles();
}

/* ==========================================
   TRAFFIC SIGNAL RENDERING
========================================== */

function renderSignals() {
    simulationState.roads.forEach(road => {
        const signal = document.getElementById(`signal-${road.id}`);

        if (!signal) return;

        const red = signal.querySelector(".light.red");
        const yellow = signal.querySelector(".light.yellow");
        const green = signal.querySelector(".light.green");

        red.classList.toggle("active", !road.isGreen);
        yellow.classList.remove("active");
        green.classList.toggle("active", road.isGreen);
    });
}

/* ==========================================
   ROAD INFORMATION
========================================== */

function renderRoads() {
    const container = document.getElementById("roadDetails");

    container.innerHTML = "";

    simulationState.roads.forEach(road => {
        const incoming = road.lanes.find(
            lane => lane.direction === "Incoming"
        );

        const outgoing = road.lanes.find(
            lane => lane.direction === "Outgoing"
        );

        const incomingCount = incoming ? incoming.vehicles.length : 0;
        const outgoingCount = outgoing ? outgoing.vehicles.length : 0;

        const card = document.createElement("div");

        card.className = "road-card";

        card.innerHTML = `
            <h3>${road.name}</h3>
            <div>Signal: <strong>${road.isGreen ? "GREEN" : "RED"}</strong></div>
            <div>Incoming: ${incomingCount}</div>
            <div>Outgoing: ${outgoingCount}</div>
            <div>Total: ${incomingCount + outgoingCount}</div>
        `;

        container.appendChild(card);
    });
}

/* ==========================================
   VEHICLE POSITION CALCULATION
========================================== */

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}

function getVehiclePosition(roadId, direction, distance, lane) {
    const progress = clamp(
        direction === "Incoming"
            ? (100 - distance) / 100
            : distance / 100,
        0,
        1
    );

    const vertical = roadId === "North" || roadId === "South";

    const width = lane.clientWidth;
    const height = lane.clientHeight;

    const vehicleWidth = vertical ? 23 : 35;
    const vehicleHeight = vertical ? 35 : 23;

    const maxX = Math.max(0, width - vehicleWidth);
    const maxY = Math.max(0, height - vehicleHeight);

    let x = maxX / 2;
    let y = maxY / 2;

    if (roadId === "North") {
        if (direction === "Incoming") {
            const stopY = clamp(height - 90 - 91 - vehicleHeight - 12, 0, maxY);
            y = progress * stopY;
        } else {
            y = (1 - progress) * maxY;
        }
    }

    if (roadId === "South") {
        if (direction === "Incoming") {
            const stopY = clamp(100 + 91 + 12, 0, maxY);
            y = maxY - progress * (maxY - stopY);
        } else {
            y = progress * maxY;
        }
    }

    if (roadId === "East") {
        if (direction === "Incoming") {
            const stopX = clamp(120 + 35 + 12, 0, maxX);
            x = maxX - progress * (maxX - stopX);
        } else {
            x = progress * maxX;
        }
    }

    if (roadId === "West") {
        if (direction === "Incoming") {
            const stopX = clamp(width - 120 - 35 - vehicleWidth - 12, 0, maxX);
            x = progress * stopX;
        } else {
            x = (1 - progress) * maxX;
        }
    }

    return {
        x: x,
        y: y,
        horizontal: !vertical
    };
}

/* ==========================================
   VEHICLE RENDERING
========================================== */

function renderVehicles() {
    // Clear old vehicle elements
    document.querySelectorAll(".vehicle-container").forEach(container => {
        container.replaceChildren();
    });

    simulationState.roads.forEach(road => {
        road.lanes.forEach(laneData => {
            const laneId =
                `${road.id}-${laneData.direction.toLowerCase()}`;

            const laneElement = document.getElementById(laneId);

            if (!laneElement) return;

            const container =
                laneElement.querySelector(".vehicle-container");

            if (!container) return;

            laneData.vehicles.forEach(vehicle => {
                const element = document.createElement("div");

                const vehicleType = vehicle.type.toLowerCase();

                element.className = `vehicle ${vehicleType}`;

                element.title =
                    `${vehicle.vehicleNumber} | ${vehicle.type} | ${vehicle.status}`;

                const position = getVehiclePosition(
                    road.id,
                    laneData.direction,
                    vehicle.distanceToStopLine,
                    laneElement
                );

                element.style.left = `${position.x}px`;
                element.style.top = `${position.y}px`;

                if (position.horizontal) {
                    element.style.transform = "rotate(90deg)";
                }

                container.appendChild(element);
            });
        });
    });
}

/* ==========================================
   COMPLETED VEHICLES
========================================== */

function renderCompletedVehicles() {
    const container = document.getElementById("completedVehicles");

    const completed = simulationState.completedVehicles || [];

    if (completed.length === 0) {
        container.textContent = "No completed vehicles yet.";
        return;
    }

    container.innerHTML = "";

    completed.slice(-10).reverse().forEach(vehicle => {
        const item = document.createElement("div");

        item.style.padding = "7px 0";
        item.style.borderBottom = "1px solid #dce3eb";

        item.textContent =
            `${vehicle.vehicleNumber} - ${vehicle.type} - ${vehicle.roadId}`;

        container.appendChild(item);
    });
}

/* ==========================================
   EVENT LISTENERS
========================================== */

document.getElementById("startBtn")
    .addEventListener("click", startSimulation);

document.getElementById("pauseBtn")
    .addEventListener("click", pauseSimulation);

document.getElementById("resetBtn")
    .addEventListener("click", resetSimulation);

document.getElementById("addVehicleBtn")
    .addEventListener("click", addVehicle);

document.getElementById("autoGeneration")
    .addEventListener("change", changeAutoGeneration);

/* ==========================================
   INITIALIZATION
========================================== */

async function initialize() {
    await loadState();

    // Update simulation every 600 milliseconds.
    setInterval(simulationTick, 600);
}

initialize();
