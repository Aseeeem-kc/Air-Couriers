using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ACOTester : MonoBehaviour
{
    [Header("Debug / Verification")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool enableDebugGizmos = true;

    // ---------------- ACO PARAMETERS (Inspector Configurable) ----------------
    [Header("ACO Parameters")]
    [SerializeField] private float alpha = 1f;              // Importance of pheromone trail
    [SerializeField] private float beta = 0.0001f;          // Importance of heuristic distance
    [SerializeField] private float Q = 0.0006f;            // Pheromone deposit constant
    [SerializeField] private float defaultPheromone = 1.0f; // Initial pheromone on all connections
    [SerializeField] private float evaporationFactor = 0.1f; // Evaporation rate (p)
    [SerializeField] private int antsPerSearch = 50;        // Number of virtual ants to run per goal search

    public float Alpha => alpha;
    public float Beta => beta;
    public float QValue => Q;

    // ---------------- AGENT CONFIGURATION ----------------
    [Header("Agent Configuration")]
    [SerializeField] private GameObject startWaypoint;
    [SerializeField] private List<GameObject> goalWaypoints = new List<GameObject>(); // Multiple goals (should match parcel count)
    [SerializeField] private int parcelCount = 1; // Total assigned parcels (set via Inspector)
    [SerializeField] private GameObject parcelPrefab; // Parcel to drop (optional visual)
    
    private int remainingParcels; // Tracks parcels still being carried

    [Header("Speed Settings (Now managed by AgentSpeed component)")]
    private AgentSpeed agentSpeed;


    public float CurrentSpeed => agentSpeed != null ? agentSpeed.currentSpeed : 0f;
    public int ParcelCount => remainingParcels; // Return remaining parcels
    public int TotalParcelCount => parcelCount; // Return initial parcel count
    public bool IsStopped => agentSpeed != null && agentSpeed.currentSpeed == 0f;

    // ---------------- PATHFINDING ----------------
    private Graph aGraph = new Graph();
    private List<Connection> allConnections = new List<Connection>();
    private List<GameObject> allWaypoints = new List<GameObject>();
    
    // Current path and state
    private List<GameObject> currentPath = new List<GameObject>();
    private int currentPathIndex = 0;
    private int currentGoalIndex = 0;
    private bool hasReachedAllGoals = false;

    // ---------------- MOVEMENT ----------------
    private bool agentMove = true;
    private bool isPausedAtGoal = false;
    private Vector3 currentTargetPos;
    private Vector3 offset = new Vector3(0, 0.3f, 0);

    // ---------------- COMPONENT REFERENCES ----------------
    private PathfindingTester pathfindingTester;
    private AirplaneAvoidance avoidance;

    // ---------------- STATUS TRACKING ----------------
    private string deliveryStatus = "Moving";
    private bool collisionDetected = false;
    private string collisionInfo = "No Collision";
    
    // ---------------- DISTANCE TRACKING ----------------
    private float totalDistanceTravelled = 0f;
    private Vector3 lastPosition;

    public string DeliveryStatus => deliveryStatus;
    public bool CollisionDetected => collisionDetected;
    public string CollisionInfo => collisionInfo;
    public float TotalDistance => totalDistanceTravelled;
    
    // Check if ACO phase is complete (switched to A*)
    public bool HasCompletedACOPhase()
    {
        // ACO is complete when it has been disabled (switched to A*)
        return !enabled && hasReachedAllGoals;
    }

    // ------------------------ START ------------------------
    void Start()
    {
        pathfindingTester = GetComponent<PathfindingTester>();
        avoidance = GetComponent<AirplaneAvoidance>();
        agentSpeed = GetComponent<AgentSpeed>();
        if (agentSpeed == null) agentSpeed = gameObject.AddComponent<AgentSpeed>();

        // Ensure PathfindingTester is disabled initially
        if (pathfindingTester != null)
        {
            pathfindingTester.enabled = false;
        }

        if (startWaypoint == null || goalWaypoints.Count == 0)
        {
            Debug.LogError($"{gameObject.name}: Start waypoint or goal waypoints not assigned!");
            enabled = false;
            return;
        }

        // Validate parcel count is set correctly
        if (parcelCount <= 0)
        {
            Debug.LogError($"{gameObject.name}: Parcel Count is {parcelCount}! You must set it in the Inspector. " +
                          $"Expected: Agent1=4, Agent2=3, Agent3=3");
            enabled = false;
            return;
        }

        // Validate goal count matches parcel count
        if (goalWaypoints.Count != parcelCount)
        {
            Debug.LogWarning($"{gameObject.name}: Goal waypoints count ({goalWaypoints.Count}) doesn't match parcel count ({parcelCount}). " +
                           $"You should have {parcelCount} goal waypoints for {parcelCount} parcels.");
        }

        // Initialize remaining parcels (use the Inspector value directly - no PackageManager needed)
        remainingParcels = parcelCount;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ACO] {gameObject.name} initialized with {parcelCount} parcels (fixed assignment)");
        }

        // Initialize distance tracking
        lastPosition = transform.position;
        totalDistanceTravelled = 0f;

        // Snap plane to flight altitude at start
        var p = transform.position;
        p.y = agentSpeed.flightAltitude;
        transform.position = p;

        // Initialize graph
        InitializeGraph();
        
        // Initialize pheromones
        InitializePheromones();

        // Calculate initial speed based on parcel count (Drop scenario: speed * 1.1^parcelCount)
        UpdateSpeed();

        // Start pathfinding to first goal
        FindPathToNextGoal();

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[ACO] {gameObject.name} START | start={startWaypoint.name} | goals={goalWaypoints.Count} | parcels={remainingParcels} | " +
                $"α={alpha} β={beta} Q={Q} p={evaporationFactor} defaultPhero={defaultPheromone} | speed={agentSpeed.currentSpeed:F2}"
            );
        }
    }

    // ------------------------ INITIALIZE GRAPH ------------------------
    private void InitializeGraph()
    {
        GameObject[] allWaypointObjects = GameObject.FindGameObjectsWithTag("WayPoint");

        foreach (GameObject waypoint in allWaypointObjects)
        {
            VisGraphWaypointManager wp = waypoint.GetComponent<VisGraphWaypointManager>();
            if (wp != null)
            {
                allWaypoints.Add(waypoint);

                foreach (VisGraphConnection visCon in wp.Connections)
                {
                    if (visCon.ToNode != null)
                    {
                        Connection c = new Connection();
                        c.FromNode = waypoint;
                        c.ToNode = visCon.ToNode;
                        c.Pheromone = defaultPheromone;
                        allConnections.Add(c);
                        aGraph.AddConnection(c);
                    }
                }
            }
        }

        Debug.Log($"{gameObject.name}: Graph initialized with {allConnections.Count} connections");
    }

    // ------------------------ INITIALIZE PHEROMONES ------------------------
    private void InitializePheromones()
    {
        foreach (Connection c in allConnections)
        {
            c.Pheromone = defaultPheromone;
        }
    }

    // ------------------------ UPDATE SPEED ------------------------
    private void UpdateSpeed()
    {
        // Drop scenario: Currentspeed * 1.1^RemainingParcelcount
        // Speed decreases as parcels are delivered
        // Maximum 90% increase: 1.1^remainingParcels capped at 1.9
        float speedMultiplier = Mathf.Pow(1.1f, remainingParcels);
        float maxMultiplier = 1.9f; // 90% increase = 1.9x
        speedMultiplier = Mathf.Min(speedMultiplier, maxMultiplier);
        
        // normalSpeed = baseSpeed * speedMultiplier
        agentSpeed.normalSpeed = agentSpeed.baseSpeed * speedMultiplier;
        agentSpeed.currentSpeed = agentSpeed.normalSpeed; // Update both
        Debug.Log($"{gameObject.name}: Speed updated to {agentSpeed.currentSpeed:F2} (remaining parcels: {remainingParcels}, multiplier: {speedMultiplier:F2})");
    }

    // ------------------------ ACO PATHFINDING ------------------------
    private void FindPathToNextGoal()
    {
        if (currentGoalIndex >= goalWaypoints.Count)
        {
            hasReachedAllGoals = true;
            SwitchToAStar();
            return;
        }

        GameObject currentGoal = goalWaypoints[currentGoalIndex];
        GameObject currentStart = currentPathIndex < currentPath.Count 
            ? currentPath[currentPathIndex] 
            : startWaypoint;

        // Use ACO to find path from current position to next goal
        List<GameObject> path = ACOPathfind(currentStart, currentGoal);
        
        if (path.Count > 0)
        {
            // If we're already on a path, append from current position
            if (currentPathIndex > 0 && currentPath.Count > 0)
            {
                currentPath.RemoveRange(0, currentPathIndex);
                currentPathIndex = 0;
            }
            
            currentPath = path;
            currentPathIndex = 0;
            deliveryStatus = "Moving";

            if (enableDebugLogs)
                Debug.Log($"[ACO] {gameObject.name} PATH → goal#{currentGoalIndex + 1}/{goalWaypoints.Count} ({currentGoal.name}) nodes={currentPath.Count}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: ACO could not find path to goal {currentGoalIndex}");
            hasReachedAllGoals = true;
            SwitchToAStar();
        }
    }

    // ------------------------ ACO ALGORITHM (Multi-Ant Version) ------------------------
    private List<GameObject> ACOPathfind(GameObject start, GameObject goal)
    {
        List<GameObject> bestPath = new List<GameObject>();
        float shortestDistance = float.MaxValue;

        if (enableDebugLogs)
            Debug.Log($"[ACO] {gameObject.name} starting search for {goal.name} with {antsPerSearch} virtual ants...");

        for (int i = 0; i < antsPerSearch; i++)
        {
            // Run a virtual ant walk
            List<GameObject> currentAntPath = WalkSingleAnt(start, goal);
            
            if (currentAntPath.Count > 0)
            {
                // Calculate distance of this specific path
                float distance = GetPathDistance(currentAntPath);
                
                // Check if it's the new shortest path
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    bestPath = new List<GameObject>(currentAntPath);
                }
            }
        }

        // Evaporate pheromones once after all virtual ants have finished their walks
        EvaporatePheromones();

        if (bestPath.Count > 0)
        {
            if (enableDebugLogs)
                Debug.Log($"{gameObject.name}: ACO search complete. Shortest path found: {shortestDistance:F2}m ({bestPath.Count} nodes)");
            return bestPath;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: ACO multi-ant search failed to find any valid path to {goal.name}");
            return new List<GameObject>();
        }
    }

    private List<GameObject> WalkSingleAnt(GameObject start, GameObject goal)
    {
        List<GameObject> path = new List<GameObject>();
        
        if (start == null || goal == null || start.Equals(goal))
        {
            path.Add(start);
            return path;
        }

        GameObject currentNode = start;
        HashSet<GameObject> visited = new HashSet<GameObject>();
        path.Add(currentNode);

        int maxPathLength = 200; // Limit ant path length to prevent infinite loops in complex graphs
        int steps = 0;

        while (!currentNode.Equals(goal) && steps < maxPathLength)
        {
            steps++;
            visited.Add(currentNode);

            List<Connection> connections = aGraph.GetConnections(currentNode);
            if (connections.Count == 0) break;

            List<Connection> candidates = new List<Connection>();
            List<float> probabilities = new List<float>();
            float totalProbability = 0f;

            foreach (Connection conn in connections)
            {
                if (conn == null || conn.ToNode == null) continue;
                if (visited.Contains(conn.ToNode)) continue;

                float distToGoal = Vector3.Distance(conn.ToNode.transform.position, goal.transform.position);
                if (distToGoal <= 0f) distToGoal = 0.001f;

                // ACO probability formula
                float pheromoneValue = Mathf.Pow(conn.Pheromone, alpha);
                float heuristicValue = Mathf.Pow(1f / distToGoal, beta);
                float probability = pheromoneValue * heuristicValue;

                if (probability <= 0f) continue;

                candidates.Add(conn);
                probabilities.Add(probability);
                totalProbability += probability;
            }

            if (candidates.Count == 0 || totalProbability <= 0f) break;

            // Selection
            float randomValue = Random.Range(0f, totalProbability);
            float cumulative = 0f;
            int selectedIndex = 0;

            for (int j = 0; j < candidates.Count; j++)
            {
                cumulative += probabilities[j];
                if (randomValue <= cumulative)
                {
                    selectedIndex = j;
                    break;
                }
            }

            Connection selectedConnection = candidates[selectedIndex];
            
            // Local Pheromone Update: Virtual ant deposits pheromone on the edge it chose
            selectedConnection.Pheromone += Q;

            currentNode = selectedConnection.ToNode;
            path.Add(currentNode);
        }

        return currentNode.Equals(goal) ? path : new List<GameObject>();
    }

    private float GetPathDistance(List<GameObject> path)
    {
        float totalDist = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (path[i] != null && path[i+1] != null)
            {
                totalDist += Vector3.Distance(path[i].transform.position, path[i+1].transform.position);
            }
        }
        return totalDist;
    }

    // ------------------------ EVAPORATE PHEROMONES ------------------------
    private void EvaporatePheromones()
    {
        foreach (Connection c in allConnections)
        {
            c.Pheromone = c.Pheromone * (1f - evaporationFactor);
            c.Pheromone = Mathf.Max(c.Pheromone, 0.01f); // Keep minimum pheromone
        }
    }

    // ------------------------ UPDATE ------------------------
    void Update()
    {
        // Distance tracking (always track, even when stopped)
        float frameDistance = Vector3.Distance(transform.position, lastPosition);
        totalDistanceTravelled += frameDistance;
        lastPosition = transform.position;

        if (!agentMove || hasReachedAllGoals)
            return;

        // Check collision avoidance
        CheckCollisionAvoidance();

        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
        {
            // Reached current goal
            if (!isPausedAtGoal)
            {
                StartCoroutine(PauseAtGoal());
            }
            return;
        }

        // Move towards current target
        currentTargetPos = currentPath[currentPathIndex].transform.position + offset;
        // Enforce flat altitude for target
        currentTargetPos.y = agentSpeed.flightAltitude;

        Vector3 directionToWaypoint = currentTargetPos - transform.position;
        float distanceToWaypoint = directionToWaypoint.magnitude;

        // Check if reached waypoint
        if (distanceToWaypoint < 20f)
        {
            currentPathIndex++;
            return;
        }

        // Apply avoidance offset to get final steering target
        Vector3 finalTarget = currentTargetPos;
        if (avoidance != null)
        {
            finalTarget += avoidance.GetOffset();
        }
        // Keep flat altitude on final target
        finalTarget.y = agentSpeed.flightAltitude;

        // Calculate direction to the final target (including avoidance)
        Vector3 steeringDirection = finalTarget - transform.position;

        // Smooth rotation towards steering target
        if (steeringDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(steeringDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                2f * Time.deltaTime
            );
        }

        // Move towards target
        transform.position = Vector3.MoveTowards(
            transform.position,
            finalTarget,
            agentSpeed.currentSpeed * Time.deltaTime
        );
        // Clamp altitude after move
        var pos = transform.position;
        pos.y = agentSpeed.flightAltitude;
        transform.position = pos;
    }

    // ------------------------ PAUSE AT GOAL ------------------------
    private IEnumerator PauseAtGoal()
    {
        isPausedAtGoal = true;
        agentMove = false;
        agentSpeed.currentSpeed = 0f; // Stop completely
        deliveryStatus = "Delivering";

        // Drop ONE parcel at this goal (in air)
        if (remainingParcels > 0)
        {
            if (enableDebugLogs)
            {
                string goalName = (currentGoalIndex >= 0 && currentGoalIndex < goalWaypoints.Count && goalWaypoints[currentGoalIndex] != null)
                    ? goalWaypoints[currentGoalIndex].name
                    : "UNKNOWN_GOAL";
                Debug.Log($"[ACO] {gameObject.name} ARRIVED goal#{currentGoalIndex + 1}/{goalWaypoints.Count} ({goalName}) | HOLD 3s | speed=0");
            }

            // Drop parcel visual effect (in air)
            GameObject parcel;
            if (parcelPrefab != null)
            {
                parcel = Instantiate(parcelPrefab, transform.position + Vector3.down * 5f, Quaternion.identity);
            }
            else
            {
                // Create a simple cube if no prefab is assigned
                parcel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                parcel.transform.position = transform.position + Vector3.down * 5f;
                parcel.transform.localScale = new Vector3(5f, 5f, 5f); // Scale it up to be visible
                
                // Color it brown like a package
                Renderer renderer = parcel.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.45f, 0.33f, 0.22f); // Brown
                }
            }

            // Add the animation script if not already present
            if (parcel.GetComponent<DroppedParcel>() == null)
            {
                parcel.AddComponent<DroppedParcel>();
            }

            // Remove any colliders so they don't interfere with planes
            Collider col = parcel.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Decrease parcel count
            remainingParcels--;
            if (enableDebugLogs)
                Debug.Log($"[ACO] {gameObject.name} DROPPED 1 parcel | remaining={remainingParcels}");

            yield return new WaitForSeconds(3f); // Hold for 3 seconds

            // Update speed based on remaining parcels
            UpdateSpeed();
            if (enableDebugLogs)
                Debug.Log($"[ACO] {gameObject.name} RESUME | speed={agentSpeed.currentSpeed:F2}");

            // Move to next goal
            currentGoalIndex++;
            isPausedAtGoal = false;
            agentMove = true;

            if (currentGoalIndex < goalWaypoints.Count && remainingParcels > 0)
            {
                FindPathToNextGoal();
            }
            else
            {
                // All parcels delivered or all goals reached
                hasReachedAllGoals = true;
                SwitchToAStar();
            }
        }
        else
        {
            // No parcels left, switch to A*
            yield return new WaitForSeconds(0.1f);
            hasReachedAllGoals = true;
            SwitchToAStar();
        }
    }

    // ------------------------ SWITCH TO A* ------------------------
    private void SwitchToAStar()
    {
        if (enableDebugLogs)
            Debug.Log($"[ACO] {gameObject.name} DONE deliveries | switching to A* return | speed=0 | disabling ACOTester, enabling PathfindingTester");

        // Set speed to exactly 0
        agentSpeed.currentSpeed = 0f;
        agentMove = false;
        deliveryStatus = "Returning";

        // Disable ACO Tester
        this.enabled = false;

        // Enable PathfindingTester
        if (pathfindingTester != null)
        {
            // Set PathfindingTester's start to current position's nearest waypoint
            // and end to original start waypoint
            pathfindingTester.enabled = true;
            
            // Find nearest waypoint to current position
            GameObject nearestWaypoint = FindNearestWaypoint(transform.position);
            if (nearestWaypoint != null)
            {
                // Use reflection or public method to set start/end
                SetPathfindingTesterStartEnd(nearestWaypoint, startWaypoint);
            }
            else
            {
                SetPathfindingTesterStartEnd(startWaypoint, startWaypoint);
            }
        }
        else
        {
            Debug.LogError($"{gameObject.name}: PathfindingTester component not found!");
        }
    }

    // ------------------------ HELPER METHODS ------------------------
    private GameObject FindNearestWaypoint(Vector3 position)
    {
        GameObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (GameObject wp in allWaypoints)
        {
            float dist = Vector3.Distance(position, wp.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = wp;
            }
        }

        return nearest;
    }

    private void SetPathfindingTesterStartEnd(GameObject start, GameObject end)
    {
        // Use public setters
        pathfindingTester.StartWaypoint = start;
        pathfindingTester.EndWaypoint = end;

        // Reinitialize PathfindingTester
        pathfindingTester.InitializePathfinding();
    }

    // ------------------------ COLLISION AVOIDANCE ------------------------
    private void CheckCollisionAvoidance()
    {
        collisionDetected = false;
        collisionInfo = "No Collision";

        // Find other agents
        ACOTester[] allAgents = FindObjectsOfType<ACOTester>();
        PathfindingTester[] astarAgents = FindObjectsOfType<PathfindingTester>();
        
        bool shouldAvoid = false;
        string avoidingAgentName = "";

        // Check against ACO agents
        foreach (ACOTester otherAgent in allAgents)
        {
            if (otherAgent == this || !otherAgent.enabled)
                continue;

            float distance = Vector3.Distance(transform.position, otherAgent.transform.position);
            float safeDistance = 70f; // Same as AirplaneAvoidance

            if (distance < safeDistance)
            {
                float otherSpeed = otherAgent.CurrentSpeed;
                
                // If I'm slower, I need to avoid
                if (agentSpeed.normalSpeed < otherSpeed)
                {
                    shouldAvoid = true;
                    avoidingAgentName = otherAgent.gameObject.name;
                    break;
                }
            }
        }

        // Check against A* agents
        if (!shouldAvoid)
        {
            foreach (PathfindingTester otherAgent in astarAgents)
            {
                if (otherAgent.gameObject == gameObject || !otherAgent.enabled)
                    continue;

                float distance = Vector3.Distance(transform.position, otherAgent.transform.position);
                float safeDistance = 70f;

                if (distance < safeDistance)
                {
                    float otherSpeed = otherAgent.CurrentSpeed;
                    
                    if (agentSpeed.normalSpeed < otherSpeed)
                    {
                        shouldAvoid = true;
                        avoidingAgentName = otherAgent.gameObject.name;
                        break;
                    }
                }
            }
        }

        // Apply avoidance
        if (shouldAvoid)
        {
            collisionDetected = true;
            collisionInfo = $"Avoiding {avoidingAgentName}";
            agentSpeed.currentSpeed = agentSpeed.normalSpeed * 0.5f; // Reduce speed by 50%
            deliveryStatus = "Avoiding";
        }
        else
        {
            // Restore normal speed when no collision
            agentSpeed.currentSpeed = agentSpeed.normalSpeed;
            if (deliveryStatus == "Avoiding")
            {
                deliveryStatus = "Moving";
            }
        }
    }

    // ------------------------ GIZMOS ------------------------
    void OnDrawGizmos()
    {
        if (!enableDebugGizmos) return;
        if (currentPath == null || currentPath.Count == 0)
            return;

        // Green = ACO path (delivery phase)
        Gizmos.color = Color.green;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            if (currentPath[i] != null && currentPath[i + 1] != null)
            {
                Gizmos.DrawLine(
                    currentPath[i].transform.position + offset,
                    currentPath[i + 1].transform.position + offset
                );
            }
        }
    }
}

