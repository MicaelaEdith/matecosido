using UnityEngine;

public class IdleVariationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string baseIdleStateName = "Idle";
    [SerializeField] private string[] variationTriggers = { "PlayZombie", "PlayWarrior", "PlayDwarf" };
    [SerializeField] private int loopsBeforeVariation = 2;

    private int loopsInBaseIdle;
    private float previousNormalizedTime;
    private bool wasInBaseIdle;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (animator == null || variationTriggers == null || variationTriggers.Length == 0)
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (!state.IsName(baseIdleStateName))
        {
            wasInBaseIdle = false;
            return;
        }

        float normalizedTime = Mathf.Repeat(state.normalizedTime, 1f);

        if (!wasInBaseIdle)
        {
            wasInBaseIdle = true;
            previousNormalizedTime = normalizedTime;
            return;
        }

        if (normalizedTime < previousNormalizedTime)
        {
            loopsInBaseIdle++;

            if (loopsInBaseIdle >= Mathf.Max(1, loopsBeforeVariation))
            {
                loopsInBaseIdle = 0;
                wasInBaseIdle = false;
                animator.SetTrigger(variationTriggers[Random.Range(0, variationTriggers.Length)]);
                return;
            }
        }

        previousNormalizedTime = normalizedTime;
    }
}
