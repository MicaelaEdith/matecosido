using UnityEngine;

public class PlayerWeaponHandler : MonoBehaviour
{
    [Header("Weapon Models")]
    [SerializeField] private GameObject pistol;
    [SerializeField] private GameObject knife;

    [Header("References")]
    [SerializeField] private PlayerController playerController;

    [Header("Settings")]
    [SerializeField] private bool showPistolOnlyInCombat;

    private GameManager gameManager;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (playerController != null)
            playerController.WeaponEquippedChanged += HandleWeaponEquippedChanged;

        if (gameManager != null)
            gameManager.CombatStateChanged += HandleCombatStateChanged;

        RefreshWeapons();
    }

    private void OnDestroy()
    {
        if (playerController != null)
            playerController.WeaponEquippedChanged -= HandleWeaponEquippedChanged;

        if (gameManager != null)
            gameManager.CombatStateChanged -= HandleCombatStateChanged;
    }

    private void HandleWeaponEquippedChanged(bool knifeEquipped)
    {
        RefreshWeapons();
    }

    private void HandleCombatStateChanged(bool inCombat)
    {
        RefreshWeapons();
    }

    private void RefreshWeapons()
    {
        bool knifeEquipped = playerController != null && playerController.IsKnifeEquipped;
        bool inCombat = gameManager != null && gameManager.InCombat;
        bool showPistol = !knifeEquipped && (!showPistolOnlyInCombat || inCombat);

        if (knife != null)
            knife.SetActive(knifeEquipped);

        if (pistol != null)
            pistol.SetActive(showPistol);
    }
}
