# Restaurant Direction Assistant System: Analysis & Plan

## 1. System Architecture & ROI Estimation (Simulation Phase)
Based on team discussions, the project includes a critical **Simulation & Estimation Phase** before physical deployment to validate layouts and costs.

### A. Simulation Engine
*   **Purpose:** To validate restaurant layouts and determine optimal robot fleet size without physical risks.
*   **Features:**
    *   **Time Control:** Ability to "Accelerate" time to simulate a full day's service in minutes.
    *   **Collision Testing:** Run "Collision Algorithms" in a virtual environment to ensure layout safety.
    *   **Entities:** Virtual representations of **Tables**, **Bots**, and **Guests**.

### B. ROI & Fleet Estimation
*   **Goal:** Answer the business question: *"How many bots do we actually need?"*
*   **Logic:**
    *   **Input:** Table count, Guest turnover rate, Layout map.
    *   **Process:** Run simulation with 1, 2, ... N bots.
    *   **Output:** Throughput analysis, Wait times, and **Cost Assessment (ROI/Saving)**.

---

## 2. Core Operational Entities & Roles
Reflecting the system architecture and workflow, the simulation manages the following key entities:

### A. Guest
*   **Role:** The customer requiring service.
*   **Properties:** Group size, patience level, eating duration.
*   **Lifecycle:** `Arrival (Reception)` -> `Escort (Robot)` -> `Seating (Table)` -> `Dining` -> `Departure (Exit)` (triggering "Dirty Table" state).

### B. Robot (The Waiter)
*   **Role:** The autonomous agent performing physical transport tasks.
*   **Properties:** Battery level, current payload, state (Idle, Moving, Blocked), location.
*   **Task Queue:** Receives tasks from the Central Dispatcher (e.g., "Go to Dispensing Counter", "Go to Table 5").

### C. Table
*   **Role:** The physical destination for guests and deliveries.
*   **Properties:** ID, capacity (seats), location coordinates.
*   **State Machine:** `Available` -> `Occupied (Waiting)` -> `Occupied (Eating)` -> `Dirty` -> `Available`.

### D. Dispensing Counter (Kitchen / Bar)
*   **Role:** The origin point for food and drink deliveries.
*   **Function:**
    *   **Order Queue:** Holds prepared items waiting for a robot.
    *   **Interaction:** Staff loads the robot here and confirms logic dispatch.

### E. Pick-up Counter (Dish Return / Wash Station)
*   **Role:** The destination for used tableware and trash.
*   **Function:**
    *   **Unloading:** Robot arrives here full of dirty dishes.
    *   **Interaction:** Staff unloads the robot and releases it back to the "Idle" pool.

### F. Reception / Entrance & Exit
*   **Role:** The boundary of the simulation world.
*   **Function:**
    *   **Guest Source/Sink:** Simulates guests arriving (queueing) and leaving (freeing up the table).
    *   **Escort Start Point:** Robots meet new guests here to lead them to tables.

### G. Restrooms (Target Point)
*   **Role:** A specific navigation target for guests.
*   **Function:**
    *   **Guidance Destination:** Robots may receive requests to guide guests here ("Follow me to the restrooms").
    *   **Patrol Point:** Robots may cruise by to check for cleaning needs.

---

## 3. Robot Capabilities & Use Cases (Hardware Agnostic)

Since the specific robot hardware might change, here is a revised, **hardware-agnostic summary** of the common tasks. These represent the standard API concepts you will likely find in almost any autonomous mobile robot SDK for service industries.

### 1. Point-to-Point Navigation
The core capability of any service robot is moving autonomously to a specific, pre-defined location.
*   **Concept:** `NavigateTo(TargetID)`
*   **Restaurant Tasks:**
    *   **Food Delivery:** Waiter places food on the tray and sends the robot from "Kitchen" to "Table 5".
    *   **Dish Return:** Staff loads dirty dishes at a table and sends the robot to "Dishwashing Area".
    *   **Escorting:** The robot leads guests from "Reception" to "Table 12".

### 2. Path Cruising / Patrolling
Instead of a single destination, the robot follows a specific route consisting of multiple points, often in a loop.
*   **Concept:** `StartPatrol(RouteID, LoopMode)`
*   **Restaurant Tasks:**
    *   **Mobile Service:** Moving slowly through the dining area offering snacks, napkins, or taking trash.
    *   **Marketing:** Carrying a digital signboard or menu around the waiting area.
    *   **Security/Safety:** Doing a closing round to check the floor.

### 3. Auto-Docking & Power Management
The ability for the robot to manage its own energy levels without human intervention.
*   **Concept:** `ReturnToCharger()`
*   **Restaurant Tasks:**
    *   **Idle Behavior:** Automatically return to the dock when there are no active orders to keep aisles clear.
    *   **Shift End:** Self-park and recharge at closing time.

### 4. Status & Event Monitoring
The robot provides real-time feedback to the central system to coordinate with human staff.
*   **Concept:** `OnStatusChange(Event)`
*   **Restaurant Tasks:**
    *   **Arrival Notification:** "Robot has arrived at Table 5" (Trigger UI popup for guests to take food).
    *   **Obstacle Alert:** "Robot is stuck near the bar" (Notify staff to help).
    *   **Task Completion:** "delivery finished" (Robot is ready for the next order).

### 5. Map & Location Management
The API allows the system to understand the physical layout of the restaurant.
*   **Concept:** `GetLocations()` / `LoadMap(MapID)`
*   **Restaurant Tasks:**
    *   **Dynamic Layouts:** Switching map profiles for "Event Mode" vs "Regular Dining".
    *   **Table Management:** Fetching the list of active table coordinates to populate the waiter's control app.

---

## 4. Implementation Analysis: Challenges, Dependencies & Risks

### A. Dependencies
1.  **Network Infrastructure:** High-quality Wi-Fi with low latency is critical to prevent robots from becoming "brain-dead" or losing coordination.
2.  **Physical Environment:** Flooring must be level and non-reflective. Aisle width must accommodate passing traffic (>80cm).
3.  **Kitchen Workflow:** A dedicated "Parking/Loading Zone" is required that doesn't obstruct human traffic.
4.  **Map Maintenance:** Up-to-date digital maps are required to handle layout changes (e.g., moving tables for parties).

### B. Technical Challenges
1.  **Routing Algorithm:** Calculating efficient paths in a dynamic environment without deadlocks.
2.  **Dynamic Obstacle / Collision Avoidance:** Real-time reaction to chaotic elements like children running or chairs being pulled out.
3.  **Localization Drift ("The Kidnap Problem"):** Handling accuracy loss in crowded rooms where static walls are blocked from view.
4.  **Fleet Coordination:** Preventing jams when multiple robots meet in narrow corridors (prioritizing food delivery over empty return trips).

### C. Risks & Mitigations

| Risk Category | Specific Risk | Mitigation Strategy |
| :--- | :--- | :--- |
| **Safety** | Spilling hot food/drinks on sudden stop. | **Velocity Profiles:** Implement "Liquid Mode" (smooth accel/decel). <br> **LIDAR Safety Zones:** Increase detection range to slow down earlier. |
| **Operational** | Robot dying mid-service in a walkway. | **Battery Thresholds:** Force return-to-base at 20%. <br> **Manual Override:** Physical "Freewheel" button for manual pushing. |
| **Deadlocks** | Robots blocking each other. | **Central Dispatch:** Priority logic (food > empty) and traffic rules. |
| **User Experience** | Customers unsure how to interact. | **Voice/Visual Cues:** Clear audio instructions and one-touch UI confirmation. |
| **Hygiene** | Robot surfaces becoming dirty. | **Material:** Food-grade, easy-to-wipe materials and scheduled sanitization. |
