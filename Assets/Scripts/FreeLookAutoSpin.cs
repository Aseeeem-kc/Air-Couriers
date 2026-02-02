using UnityEngine;

public class FreeLookAutoSpin : MonoBehaviour
{
    public Transform pivot;       // Assign the Pivot under FreeLook Camera
    public float spinSpeed = 30f; // degrees per second

    void Update()
    {
        if (pivot != null)
        {
            // Rotate pivot around Y-axis continuously
            pivot.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}
