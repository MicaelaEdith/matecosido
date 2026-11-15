using UnityEngine;

public class PoliceWeaponHandler : MonoBehaviour
{
    private static readonly int IsMeleeEquippedParameter = Animator.StringToHash("IsMeleeEquipped");

    [Header("Weapon Models")]
    [SerializeField] private GameObject pistol;
    [SerializeField] private GameObject cachiporra;

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("State")]
    [SerializeField] private bool startWithCachiporra;

    private bool cachiporraEquipped;
    private bool weaponsVisible = true;

    public bool CachiporraEquipped => cachiporraEquipped;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        SetCachiporraEquipped(startWithCachiporra);
    }

    public void SetCachiporraEquipped(bool equipped)
    {
        cachiporraEquipped = equipped;

        if (animator != null)
            animator.SetBool(IsMeleeEquippedParameter, cachiporraEquipped);

        RefreshWeapons();
    }

    public void SetWeaponsVisible(bool visible)
    {
        weaponsVisible = visible;
        RefreshWeapons();
    }

    private void RefreshWeapons()
    {
        if (cachiporra != null)
            cachiporra.SetActive(weaponsVisible && cachiporraEquipped);

        if (pistol != null)
            pistol.SetActive(weaponsVisible && !cachiporraEquipped);
    }

    [ContextMenu("Toggle Cachiporra")]
    private void ToggleCachiporraFromContextMenu()
    {
        SetCachiporraEquipped(!cachiporraEquipped);
    }
}
