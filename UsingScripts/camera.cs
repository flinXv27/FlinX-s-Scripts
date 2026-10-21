using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector2 locationOffset;

    void LateUpdate()
    {
        Vector3 desiredPosition = target.position + (Vector3)locationOffset;
        desiredPosition.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position,desiredPosition,smoothSpeed);
    }
}
// thank you random user from stack overflow!