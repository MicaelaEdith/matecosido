using UnityEngine;

public class NpcController : MonoBehaviour
{
    private static readonly int IsSadParameter = Animator.StringToHash("IsSad");
    private static readonly int IsTalkingParameter = Animator.StringToHash("IsTalking");
    private static readonly int IsWaitingParameter = Animator.StringToHash("IsWaiting");
    private static readonly int IsRunningParameter = Animator.StringToHash("IsRunning");
    private static readonly int IsThankfulParameter = Animator.StringToHash("IsThankful");

    [SerializeField] private Animator animator;
    [SerializeField] private bool startSad = true;
    [SerializeField] private float turnSpeed = 6f;

    private bool isSad;
    private bool isTalking;
    private bool isWaiting;
    private bool isRunning;
    private bool isThankful;
    private Transform lookTarget;
    private float yawOffset;

    public bool IsSad => isSad;
    public bool IsTalking => isTalking;
    public bool IsWaiting => isWaiting;
    public bool IsRunning => isRunning;
    public bool IsThankful => isThankful;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        SetSad(startSad);
        SetTalking(false);
    }

    private void Update()
    {
        if (lookTarget == null)
            return;

        Vector3 direction = lookTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction) * Quaternion.Euler(0f, yawOffset, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);
    }

    public void SetSad(bool sad)
    {
        isSad = sad;

        if (animator != null)
            animator.SetBool(IsSadParameter, isSad);
    }

    public void SetTalking(bool talking)
    {
        isTalking = talking;

        if (animator != null)
            animator.SetBool(IsTalkingParameter, isTalking);
    }

    public void SetWaiting(bool waiting)
    {
        isWaiting = waiting;

        if (animator != null)
            animator.SetBool(IsWaitingParameter, isWaiting);
    }

    public void SetRunning(bool running)
    {
        isRunning = running;

        if (animator != null)
            animator.SetBool(IsRunningParameter, isRunning);
    }

    public void SetThankful(bool thankful)
    {
        isThankful = thankful;

        if (animator != null)
            animator.SetBool(IsThankfulParameter, isThankful);
    }

    public void FaceTarget(Transform target, float yaw = 0f)
    {
        lookTarget = target;
        yawOffset = yaw;
    }

    public void ClearLookTarget()
    {
        lookTarget = null;
    }
}
