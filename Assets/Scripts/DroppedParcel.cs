using UnityEngine;

public class DroppedParcel : MonoBehaviour
{
    private float fallSpeed = 30f;
    private float rotationSpeed = 100f;
    private float lifeTime = 5f;
    private Vector3 rotationAxis;

    void Start()
    {
        // Random rotation axis for variety
        rotationAxis = new Vector3(Random.value, Random.value, Random.value).normalized;
        
        // Auto-destroy after 5 seconds to keep scene clean
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Simple downward movement
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // Rotate while falling
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);

        // Subtle shrinking over time
        transform.localScale *= (1f - 0.2f * Time.deltaTime);
    }
}
