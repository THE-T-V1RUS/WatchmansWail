using UnityEngine;

public class Hover : MonoBehaviour
{
    [Header("Hover Settings")]
    public float amplitude = 0.5f;   // How far up/down it moves
    public float frequency = 1f;     // How fast it moves

    private Vector3 startPos;

    void Start()
    {
        // Save the starting position
        startPos = transform.position;
    }

    void Update()
    {
        // Calculate new Y offset using sine wave
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;

        // Apply position
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}
