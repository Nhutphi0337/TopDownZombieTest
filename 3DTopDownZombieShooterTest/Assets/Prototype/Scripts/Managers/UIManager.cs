using UnityEngine;
public class UIManager : MonoBehaviour
{
    [field: SerializeField] public PlayerUI PlayerUI { get; private set; }
    [field: SerializeField] public LevelDoneUI LevelDoneUI { get; private set; }
    [field: SerializeField] public Transform PlayerControlTransform { get; private set; }
    [field: SerializeField] public Transform LevelSelectionUI { get; private set; }
    
    public void PrepareUIsOnStartingLevel()
    {
        PlayerUI.gameObject.SetActive(true);
        PlayerControlTransform.gameObject.SetActive(true);
        LevelSelectionUI.transform.gameObject.SetActive(false);
        LevelDoneUI.gameObject.SetActive(false);
    }
    public void PrepareUIsOnDoneLevel()
    {
        PlayerUI.gameObject.SetActive(false);
        PlayerControlTransform.gameObject.SetActive(false);
        LevelDoneUI.gameObject.SetActive(true);
    }
    public void PrepareUIsOnFailLevel()
    {
        PlayerUI.gameObject.SetActive(false);
        PlayerControlTransform.gameObject.SetActive(false);
        LevelDoneUI.gameObject.SetActive(true);
    }
    public void SetPlayerCallBacks(Player player)
    {
        player.OnTakeDamage += PlayerUI.SetHealth;
        player.OnHeal += PlayerUI.SetHealth;
        
        player.Equipment.OnCurrentAmmoAmountChanged += PlayerUI.SetEquippedAmmo;
        player.Equipment.OnCurrentGrenadeAmountChanged += PlayerUI.SetEquippedGrenadeAmount;
        player.Equipment.OnCurrentGunActivate += PlayerUI.OnPlayerActivateGun;
        player.Equipment.OnCurrentGrenadeActivate += PlayerUI.OnPlayerActivateGrenade;

        player.GunController.OnShoot += PlayerUI.OnPlayerShoot;
        player.GunController.OnReloadDone += PlayerUI.OnPlayerReload;
    }

    public void UnSetPlayerCallBacks(Player player)
    {
        player.OnTakeDamage -= PlayerUI.SetHealth;
        player.OnHeal -= PlayerUI.SetHealth;

        player.Equipment.OnCurrentAmmoAmountChanged -= PlayerUI.SetEquippedAmmo;
        player.Equipment.OnCurrentGrenadeAmountChanged -= PlayerUI.SetEquippedGrenadeAmount;
        player.Equipment.OnCurrentGunActivate -= PlayerUI.OnPlayerActivateGun;
        player.Equipment.OnCurrentGrenadeActivate -= PlayerUI.OnPlayerActivateGrenade;

        player.GunController.OnShoot -= PlayerUI.OnPlayerShoot;
        player.GunController.OnReloadDone -= PlayerUI.OnPlayerReload;
    }
}
