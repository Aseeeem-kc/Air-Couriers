using UnityEngine;

public class FlyingBirds : MonoBehaviour
{
    public float speed = 2f; // Bird speed
    public float leftX = -12f; // Left reset position
    public float rightX = 12f; // Right end position

    void Update()
    {
        // Move bird to the right
        transform.Translate(Vector3.right * speed * Time.deltaTime);

        // Reset position if it goes off screen
        if (transform.position.x > rightX)
        {
            transform.position = new Vector3(
                leftX,
                transform.position.y,
                transform.position.z
            );
        }
    }
}
