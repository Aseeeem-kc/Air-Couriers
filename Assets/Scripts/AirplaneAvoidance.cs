using UnityEngine;

public class AirplaneAvoidance : MonoBehaviour
{
    public float safeDistance = 150f;   // distance to trigger avoidance (increased for wider buffer)
    public float avoidanceOffset = 150f; // how far sideways the slow plane moves (increased to clear wings)
    public float slowdownFactor = 0.5f; // Speed reduction factor when avoiding

    private Vector3 offset = Vector3.zero;
    private bool isAvoiding = false;

    void Update()
    {
        offset = Vector3.zero;
        isAvoiding = false;

        // Find all other agents (both ACOTester and PathfindingTester)
        ACOTester[] acoAgents = FindObjectsOfType<ACOTester>();
        PathfindingTester[] astarAgents = FindObjectsOfType<PathfindingTester>();

        // Check against ACO agents
        foreach (ACOTester otherAgent in acoAgents)
        {
            if (otherAgent.gameObject == gameObject || !otherAgent.enabled)
                continue;

            CheckAndAvoid(otherAgent.transform, GetAgentSpeed(otherAgent.gameObject));
        }

        // Check against A* agents
        foreach (PathfindingTester otherAgent in astarAgents)
        {
            if (otherAgent.gameObject == gameObject || !otherAgent.enabled)
                continue;

            CheckAndAvoid(otherAgent.transform, GetAgentSpeed(otherAgent.gameObject));
        }
    }

    private void CheckAndAvoid(Transform otherTransform, float otherSpeed)
    {
        if (otherTransform == null) return;

        float dist = Vector3.Distance(transform.position, otherTransform.position);
        
        // Get my speed
        float mySpeed = GetMySpeed();

        // If I'm slower and within safe distance, avoid
        if (dist < safeDistance && mySpeed < otherSpeed)
        {
            isAvoiding = true;
            
            // Calculate direction to move aside (perpendicular to direction to other agent)
            Vector3 toOther = (otherTransform.position - transform.position).normalized;
            Vector3 rightVector = Vector3.Cross(toOther, Vector3.up).normalized;
            
            // Move sideways (removed Time.deltaTime because this is added to a target position)
            offset = rightVector * avoidanceOffset;
            
            Debug.Log($"{gameObject.name}: Collision detected! Moving aside to avoid {otherTransform.name}");
        }
    }

    private float GetMySpeed()
    {
        AgentSpeed speed = GetComponent<AgentSpeed>();
        return speed != null ? speed.currentSpeed : 0f;
    }

    private float GetAgentSpeed(GameObject agent)
    {
        AgentSpeed speed = agent.GetComponent<AgentSpeed>();
        return speed != null ? speed.currentSpeed : 0f;
    }

    public Vector3 GetOffset()
    {
        return offset;
    }

    public bool IsAvoiding()
    {
        return isAvoiding;
    }
}
