using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingTester : MonoBehaviour
{
    [Header("Debug / Verification")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Mode")]
    [Tooltip("If true: move from start→end and STOP at end (used for Part 3 return after ACO). If false: ping-pong start↔end (legacy behavior).")]
    [SerializeField] private bool stopAtEnd = true;


    // Completion flag for "returned to start and stopped" verification
    private bool completedCycle = false;

    // ---------------- UI TRACKING ----------------
    private float totalDistanceTravelled = 0f;
    [SerializeField] private int packageCount = 1;
    private Vector3 lastPosition;

    public float TotalDistance => totalDistanceTravelled;
    public int PackageCount => packageCount;

    // ---------------- SPEED SYSTEM ----------------
    [Header("Speed Settings (Now managed by AgentSpeed component)")]
    private AgentSpeed agentSpeed;
    [SerializeField] private float speedIncreasePerParcel = 0.10f; // 10% per parcel

    private int deliveredParcelCount = 0;

    public float CurrentSpeed => agentSpeed != null ? agentSpeed.currentSpeed : 0f;

    // ---------------- PATHFINDING ----------------
    private AStarManager AStarManager = new AStarManager();
    private List<GameObject> Waypoints = new List<GameObject>();
    private List<Connection> ConnectionArray = new List<Connection>();

    [SerializeField] private GameObject start;
    [SerializeField] private GameObject end;

    // Public setters for ACO switching
    public GameObject StartWaypoint 
    { 
        get { return start; } 
        set { start = value; } 
    }
    
    public GameObject EndWaypoint 
    { 
        get { return end; } 
        set { end = value; } 
    }

    // ---------------- MOVEMENT ----------------
    private int currentTarget = 0;
    private int moveDirection = 1;   // 1 = forward, -1 = backward
    private bool agentMove = true;
    private bool hasPausedAtGoal = false;

    private Vector3 currentTargetPos;
    private Vector3 OffSet = new Vector3(0, 0.3f, 0);

    public bool HasPausedAtGoal => hasPausedAtGoal;

    // ---------------- UI HELPER ----------------
    // REQUIRED by UIFlightDisplay
    public bool CurrentTargetIsStart()
    {
        return currentTarget == 0 && moveDirection == 1;
    }

    // ------------------------ START ------------------------
    void Start()
    {
        agentSpeed = GetComponent<AgentSpeed>();
        if (agentSpeed == null) agentSpeed = gameObject.AddComponent<AgentSpeed>();
        InitializePathfinding();
    }

    // Public method to initialize pathfinding (called when switching from ACO)
    public void InitializePathfinding()
    {
        if (start == null || end == null)
        {
            Debug.Log($"{gameObject.name}: No start or end waypoints assigned.");
            return;
        }

        if (start.GetComponent<VisGraphWaypointManager>() == null ||
            end.GetComponent<VisGraphWaypointManager>() == null)
        {
            Debug.Log($"{gameObject.name}: Start or End is not a waypoint!");
            return;
        }

        // Clear previous data
        Waypoints.Clear();
        AStarManager = new AStarManager();

        GameObject[] allWaypoints = GameObject.FindGameObjectsWithTag("WayPoint");

        foreach (GameObject waypoint in allWaypoints)
        {
            if (waypoint.GetComponent<VisGraphWaypointManager>())
                Waypoints.Add(waypoint);
        }

        foreach (GameObject waypoint in Waypoints)
        {
            VisGraphWaypointManager wp = waypoint.GetComponent<VisGraphWaypointManager>();

            foreach (VisGraphConnection visCon in wp.Connections)
            {
                if (visCon.ToNode != null)
                {
                    Connection c = new Connection();
                    c.FromNode = waypoint;
                    c.ToNode = visCon.ToNode;
                    AStarManager.AddConnection(c);
                }
            }
        }

        ConnectionArray = AStarManager.PathfindAStar(start, end);

        if (ConnectionArray.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}: A* could not find a path!");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[A*] {gameObject.name} PATH ready | from={start.name} to={end.name} | connections={ConnectionArray.Count}");
        }

        // Init tracking
        lastPosition = transform.position;
        // Snap plane to flight altitude at init
        var p = transform.position;
        p.y = agentSpeed.flightAltitude;
        transform.position = p;
        
        // Check if we're switching from ACO (speed should already be 0)
        // Otherwise use base speed
        if (agentSpeed.currentSpeed == 0f)
        {
            // Coming from ACO, keep speed at 0 until we start moving
            agentSpeed.currentSpeed = 0f;
        }
        else
        {
            agentSpeed.currentSpeed = agentSpeed.baseSpeed;
        }

        // Reset movement state
        currentTarget = 0;
        moveDirection = 1;
        agentMove = true;
        hasPausedAtGoal = false;
        completedCycle = false;

        if (enableDebugLogs)
            Debug.Log($"[A*] {gameObject.name} START moving | speed={(agentSpeed.currentSpeed == 0f ? "0 (will start next Update)" : agentSpeed.currentSpeed.ToString("F2"))}");
        
        // For Part 3: during A* return phase the agent should have delivered all parcels,
        // so packageCount must stay at 0 and never be randomly reassigned.
        packageCount = 0;
    }

    // ------------------------ GIZMOS ------------------------
    void OnDrawGizmos()
    {
        if (ConnectionArray == null) return;

        foreach (Connection c in ConnectionArray)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(
                c.FromNode.transform.position + OffSet,
                c.ToNode.transform.position + OffSet
            );
        }
    }

    // ------------------------ UPDATE ------------------------
    void Update()
    {
        // Distance tracking
        float frameDistance = Vector3.Distance(transform.position, lastPosition);
        totalDistanceTravelled += frameDistance;
        lastPosition = transform.position;

        if (!agentMove || ConnectionArray.Count == 0)
            return;

        // If speed is 0 and we're starting from ACO switch, begin movement
        if (agentSpeed.currentSpeed == 0f && ConnectionArray.Count > 0)
        {
            agentSpeed.currentSpeed = agentSpeed.baseSpeed; // Start moving with base speed
            if (enableDebugLogs)
                Debug.Log($"[A*] {gameObject.name} RESUME after switch | speed={agentSpeed.currentSpeed:F2}");
        }

        Connection step = ConnectionArray[currentTarget];

        currentTargetPos = moveDirection > 0
            ? step.ToNode.transform.position
            : step.FromNode.transform.position;
        // Enforce flat altitude for target
        currentTargetPos.y = agentSpeed.flightAltitude;

        Vector3 direction = currentTargetPos - transform.position;
        float distance = direction.magnitude;

        // Apply avoidance offset
        Vector3 finalTarget = currentTargetPos;
        AirplaneAvoidance avoid = GetComponent<AirplaneAvoidance>();
        if (avoid != null)
            finalTarget += avoid.GetOffset();
        // Keep flat altitude
        finalTarget.y = agentSpeed.flightAltitude;

        // Calculate steering direction based on avoidance target
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

        transform.position = Vector3.MoveTowards(
            transform.position,
            finalTarget,
            agentSpeed.currentSpeed * Time.deltaTime
        );
        // Clamp altitude after move
        var pos = transform.position;
        pos.y = agentSpeed.flightAltitude;
        transform.position = pos;

        // Waypoint reached
        if (distance < 20f)
        {
            // In Part 3 return mode we STOP at the end waypoint (original start).
            if (stopAtEnd && moveDirection == 1 && currentTarget == ConnectionArray.Count - 1)
            {
                StopNow($"[A*] {gameObject.name} ARRIVED at end | speed=0 | STOPPED");
                return;
            }

            // Legacy behavior: pause at end then ping-pong back.
            if (!stopAtEnd && !hasPausedAtGoal &&
                moveDirection == 1 &&
                currentTarget == ConnectionArray.Count - 1)
            {
                hasPausedAtGoal = true;
                StartCoroutine(PauseAtGoal());
                return;
            }

            GetNextTarget();
        }
    }

    private void StopNow(string log)
    {
        agentSpeed.currentSpeed = 0f;
        agentMove = false;
        completedCycle = true;
        if (enableDebugLogs)
            Debug.Log($"{log} | STATUS: Completed");
    }

    // ---------------- DELIVERY + SPEED BOOST ----------------
    private IEnumerator PauseAtGoal()
    {
        agentMove = false;

        deliveredParcelCount = packageCount;
        Debug.Log($"{gameObject.name} delivered {deliveredParcelCount} parcels.");

        // Speed increase: 10% × parcels
        float speedBonus = agentSpeed.baseSpeed * speedIncreasePerParcel * deliveredParcelCount;
        agentSpeed.currentSpeed += speedBonus;

        Debug.Log($"Speed increased by {speedBonus}. New speed = {agentSpeed.currentSpeed}");

        // Delivery complete
        packageCount = 0;

        yield return new WaitForSeconds(5f);

        agentMove = true;
    }

    // ---------------- NEXT WAYPOINT ----------------
    private void GetNextTarget()
    {
        currentTarget += moveDirection;

        if (currentTarget >= ConnectionArray.Count)
        {
            if (stopAtEnd)
            {
                // Shouldn't happen (we stop at end), but guard anyway.
                StopNow($"[A*] {gameObject.name} ARRIVED at end | speed=0 | STOPPED");
                return;
            }

            // Legacy ping-pong: reverse
            moveDirection = -1;
            currentTarget = ConnectionArray.Count - 1;
        }

        if (currentTarget < 0)
        {
            moveDirection = 1;
            currentTarget = 0;
            hasPausedAtGoal = false;
            
            // Reached start position - stop completely
            StopNow($"[A*] {gameObject.name} ARRIVED back at start | speed=0 | STOPPED");
        }
    }

    // ---------------- STATUS ----------------
    public string GetDeliveryStatus()
    {
        // Check if agent has completed full cycle (delivered all packages and returned to start)
        if (completedCycle)
            return "Completed";

        if (!agentMove && hasPausedAtGoal)
            return "Delivered";

        if (moveDirection == -1)
            return "Returning";

        return "In Progress";
    }

    // Check if agent has completed full cycle (returned to start and stopped)
    public bool HasCompletedCycle()
    {
        // Explicit flag set when we stop at start.
        return completedCycle;
    }
}