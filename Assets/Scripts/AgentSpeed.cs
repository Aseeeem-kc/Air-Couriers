using UnityEngine;

[DisallowMultipleComponent]
public class AgentSpeed : MonoBehaviour
{
    [Header("Speed State")]
    public float baseSpeed = 300f;
    public float currentSpeed = 300f;
    public float normalSpeed = 300f; // Speed before avoidance adjustments

    [Header("Flight Settings")]
    public float flightAltitude = 700f; // Target altitude for the agent

    // Synchronization helper to ensure values are consistent across components
    public void ResetSpeed()
    {
        currentSpeed = baseSpeed;
        normalSpeed = baseSpeed;
    }
}
