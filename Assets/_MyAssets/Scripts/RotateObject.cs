using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [Header("Rotation Axes")]
    public bool RotateX = false;
    public bool RotateY = true;
    public bool RotateZ = false;

    [Header("Rotation Speeds (deg/sec)")]
    public float SpeedX = 0.0f;
    public float SpeedY = 45.0f;
    public float SpeedZ = 0.0f;

    [Tooltip("If true, rotation direction is reversed")]
    public bool Reverse = false;

    void Update()
    {
        float direction = Reverse ? -1.0f : 1.0f;
        float delta = Time.deltaTime * direction;

        float x = RotateX ? SpeedX * delta : 0.0f;
        float y = RotateY ? SpeedY * delta : 0.0f;
        float z = RotateZ ? SpeedZ * delta : 0.0f;

        transform.Rotate(x, y, z, Space.Self);
    }
}
