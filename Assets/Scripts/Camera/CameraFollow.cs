using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 8f, -3f);
    [SerializeField] private float followSmoothTime = 0.15f;

    [Header("Rotation")]
    [SerializeField, Range(0f, 89f)] private float pitch = 45f;

    private Vector3 followVelocity;

    private void Start()
    {
        if (target == null)
            return;

        transform.SetPositionAndRotation(target.position + offset, Quaternion.Euler(pitch, 0f, 0f));
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            followSmoothTime);

        transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
