# ✈️ Air Couriers - Intelligent Autonomous Delivery Simulation

![Unity](https://img.shields.io/badge/Unity-2022.3+-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Status](https://img.shields.io/badge/Status-Active-success?style=for-the-badge)

## 📖 Overview

**Air Couriers** is a sophisticated Unity-based simulation project that models autonomous aircraft agents delivering packages across a complex terrain. The project explores the intersection of pathfinding algorithms and autonomous agent behaviors in a 3D environment.

Agents (airplanes) are tasked with picking up packages and delivering them to designated locations while navigating through obstacles and avoiding collisions with other agents.

## 🧠 Key Algorithms

This project implements and visualizes advanced algorithm concepts:

### 1. A* Pathfinding (A-Star)
The core navigation engine uses the **A* algorithm** to calculate the most efficient paths between waypoints. 
*   **File:** `AStar.cs`, `AStarManager.cs`
*   **Function:** Finds the shortest path efficiently using heuristics (`Heuristic.cs`).

### 2. Ant Colony Optimization (ACO)
An implementation of **Ant Colony Optimization** is included to demonstrate swarm intelligence for route optimization.
*   **File:** `ACOTester.cs`
*   **Function:** Simulates pheromone-based path selection to find optimal routes over time.

### 3. Obstacle & Collision Avoidance
Agents are equipped with raycast-based detection systems to avoid static terrain and dynamic obstacles (other planes).
*   **File:** `AirplaneAvoidance.cs`

## 🌟 Features

*   **Dynamic Pathfinding Graph**: A node-based graph system (`Graph.cs`, `Connection.cs`) visualizes flight paths and connections.
*   **Package Management System**: A robust system to handle package spawning, pickup, and delivery validation (`PackageManager.cs`, `DroppedParcel.cs`).
*   **Real-time Agent Telemetry**: UI elements display agent speed, status, and flight data (`UIFlightDisplay.cs`, `AgentUIRows.cs`).
*   **3D Flight Mechanics**: Realistic yet arcade-style flight controls tailored for AI agents (`AeroplaneUserControl4Axis.cs`).

## 📂 Project Structure

```bash
Assets/
├── Scripts/
│   ├── AStar.cs             # A* Implementation
│   ├── ACOTester.cs         # Ant Colony Optimization Test
│   ├── Graph.cs             # Graph Data Structures
│   ├── AirplaneAvoidance.cs # Collision Logic
│   └── PackageManager.cs    # Delivery Logic
├── Scenes/                  # Unity Scenes
└── Prefabs/                 # Agent and Parcel Prefabs
```

## 🚀 Getting Started

1.  **Clone the Repository**
    ```bash
    git clone https://github.com/Aseeeem-kc/Air-Couriers.git
    ```
2.  **Open in Unity**
    *   Launch Unity Hub.
    *   Click **Add** and select the cloned folder.
    *   Open the project (Recommended Version: Unity 2021/2022 LTS).
3.  **Run the Simulation**
    *   Open the main scene in `Assets/Scenes`.
    *   Press the **Play** button.

## 🛠️ Built With

*   [Unity Engine](https://unity.com/) - Game Development Platform
*   [C#](https://docs.microsoft.com/en-us/dotnet/csharp/) - Primary Scripting Language


*Developed by Ashim K.C.
