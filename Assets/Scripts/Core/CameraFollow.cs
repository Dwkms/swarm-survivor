using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;

    private Vector3 velocity;
    private float cameraZ;

    private void Awake()
    {
        cameraZ = transform.position.z;

        if (target == null)
        {
            Debug.LogError("CameraFollow Target is not assigned in the Inspector.", this);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // Follow after Rigidbody2D movement so the camera reads the completed player position.
        Vector3 targetPosition = new Vector3(target.position.x, target.position.y, cameraZ);

        // SmoothDamp keeps tracking behavior consistent when frame time changes under heavy enemy load.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime);
    }
}
