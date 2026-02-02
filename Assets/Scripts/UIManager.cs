using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : MonoBehaviour
{
    [Header("UI Row References")]
    public AgentUIRow[] agentRows;
    
    [Header("Agent References")]
    public ACOTester[] acoAgents; // ACO agents (primary)
    public PathfindingTester[] astarAgents; // A* agents (for return phase)
    
    [Header("Collision Info Display")]
    public TextMeshProUGUI collisionInfoText; // Drag your collision info text here

    [Header("Completion Tracking")]
    public bool autoPauseOnCompletion = true; // Automatically pause when all agents complete
    
    private bool hasPaused = false; // Track if we've already paused

    void Update()
    {
        // Update ACO agents (active during delivery phase)
        if (acoAgents != null)
        {
            for (int i = 0; i < acoAgents.Length && i < agentRows.Length; i++)
            {
                if (acoAgents[i] != null && acoAgents[i].enabled)
                {
                    agentRows[i].UpdateRow(acoAgents[i]);
                }
            }
        }

        // Update A* agents (active during return phase)
        // Note: This assumes agents switch from ACO to A*, so we check both
        if (astarAgents != null)
        {
            for (int i = 0; i < astarAgents.Length && i < agentRows.Length; i++)
            {
                if (astarAgents[i] != null && astarAgents[i].enabled)
                {
                    // Only update if ACO agent is disabled (switched to A*)
                    ACOTester acoAgent = acoAgents != null && i < acoAgents.Length ? acoAgents[i] : null;
                    if (acoAgent == null || !acoAgent.enabled)
                    {
                        agentRows[i].UpdateRow(astarAgents[i]);
                    }
                }
            }
        }

        // Update collision information
        UpdateCollisionInfo();

        // Check if all agents have completed their cycles
        if (autoPauseOnCompletion && !hasPaused)
        {
            CheckAndPauseOnCompletion();
        }
    }

    private void CheckAndPauseOnCompletion()
    {
        if (acoAgents == null || astarAgents == null)
            return;

        int totalAgents = Mathf.Max(acoAgents.Length, astarAgents.Length);
        int completedAgents = 0;

        // Check each agent
        for (int i = 0; i < totalAgents; i++)
        {
            bool agentCompleted = false;

            // Check if ACO phase is done (agent switched to A*)
            ACOTester acoAgent = i < acoAgents.Length ? acoAgents[i] : null;
            PathfindingTester astarAgent = i < astarAgents.Length ? astarAgents[i] : null;

            if (acoAgent != null && astarAgent != null)
            {
                // Agent has completed if:
                // 1. ACO phase is done (switched to A*)
                // 2. A* phase is done (returned to start and stopped)
                bool acoDone = acoAgent.HasCompletedACOPhase();
                bool astarDone = astarAgent.HasCompletedCycle();

                if (acoDone && astarDone)
                {
                    agentCompleted = true;
                }
            }

            if (agentCompleted)
            {
                completedAgents++;
            }
        }

        // If all agents completed, pause the game
        if (completedAgents == totalAgents && totalAgents > 0)
        {
            hasPaused = true;
            
            Debug.Log("========================================");
            Debug.Log("✓ ALL AGENTS COMPLETED FULL CYCLE!");
            Debug.Log($"✓ {completedAgents}/{totalAgents} agents finished:");
            
            for (int i = 0; i < totalAgents; i++)
            {
                ACOTester acoAgent = i < acoAgents.Length ? acoAgents[i] : null;
                PathfindingTester astarAgent = i < astarAgents.Length ? astarAgents[i] : null;
                
                if (acoAgent != null && astarAgent != null)
                {
                    Debug.Log($"  - {acoAgent.gameObject.name}: " +
                             $"Delivered {acoAgent.TotalParcelCount} parcels, " +
                             $"Returned to start, " +
                             $"Total distance: {astarAgent.TotalDistance:F1}m, " +
                             $"Final speed: {astarAgent.CurrentSpeed:F2} m/s");
                }
            }
            
            Debug.Log("========================================");
            Debug.Log("GAME PAUSED - Review console logs above to verify all requirements.");
            Debug.Log("========================================");
            
            // Pause the game
            Time.timeScale = 0f;
            
            // Also pause in editor if possible
            #if UNITY_EDITOR
            EditorApplication.isPaused = true;
            #endif
        }
    }

    private void UpdateCollisionInfo()
    {
        if (collisionInfoText == null) return;

        bool anyCollision = false;
        string collisionDetails = "";

        // Check ACO agents
        if (acoAgents != null)
        {
            foreach (ACOTester agent in acoAgents)
            {
                if (agent != null && agent.enabled && agent.CollisionDetected)
                {
                    anyCollision = true;
                    if (collisionDetails != "") collisionDetails += ", ";
                    collisionDetails += $"{agent.gameObject.name}: {agent.CollisionInfo}";
                }
            }
        }

        // Check A* agents
        if (astarAgents != null)
        {
            foreach (PathfindingTester agent in astarAgents)
            {
                if (agent != null && agent.enabled)
                {
                    // Check AirplaneAvoidance component
                    AirplaneAvoidance avoidance = agent.GetComponent<AirplaneAvoidance>();
                    if (avoidance != null && avoidance.IsAvoiding())
                    {
                        anyCollision = true;
                        if (collisionDetails != "") collisionDetails += ", ";
                        collisionDetails += $"{agent.gameObject.name}: Avoiding";
                    }
                }
            }
        }

        if (anyCollision)
        {
            collisionInfoText.text = $"Agent Collision Information: {collisionDetails}";
        }
        else
        {
            collisionInfoText.text = "Agent Collision Information: No Collision";
        }
    }
}
