using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Health")]
    [SerializeField]
    private Image hpFill;

    [Header("Current Equipment")]
    [SerializeField]
    private Image currentWeaponIcon;

    [SerializeField]
    private Image currentGrenadeIcon;

    [Header("Secondary Equipment")]
    [SerializeField]
    private Image secondaryWeaponIcon;

    [SerializeField]
    private Image secondaryGrenadeIcon;

    [Header("Ammo")]
    [SerializeField]
    private TMP_Text equippedAmmoTxt;
    [SerializeField]
    private TMP_Text currentAmmoTxt;

    [Header("Grenade")]
    [SerializeField]
    private TMP_Text equippedGrenadeTxt;

    public void OnPlayerActivateGun(Gun curGun, Gun secondGun, int curGunStorageAmmo)
    {
        SetCurrentAmmo(curGun.CurrentAmmo);
        SetEquippedAmmo(curGunStorageAmmo);

        SetCurrentGun(curGun.GunDef.Icon);
        SetSecondaryGun(secondGun.GunDef.Icon);
    }
    public void OnPlayerActivateGrenade(GrenadeDef curGrenade, GrenadeDef secondGrenade, int curGrenadeStorage)
    {
        SetEquippedGrenadeAmount(curGrenadeStorage);
        SetCurrentGrenade(curGrenade.Icon);
        SetSecondaryGrenade(secondGrenade.Icon);
    }
    public void OnPlayerShoot(Gun gun)
    {
        SetCurrentAmmo(gun.CurrentAmmo);
    }
    public void OnPlayerReload(Gun gun, int storageAmmo)
    {
        SetCurrentAmmo(gun.CurrentAmmo);
        SetEquippedAmmo(storageAmmo);
    }
    public void SetHealth(float current, float prev, float max)
    {
        if (max <= 0f)
        {
            hpFill.fillAmount = 0f;
            return;
        }

        hpFill.fillAmount = Mathf.Clamp01(current / max);
    }

    public void SetCurrentGun(Sprite icon)
    {
        SetIcon(currentWeaponIcon, icon);
    }

    public void SetCurrentGrenade(Sprite icon)
    {
        SetIcon(currentGrenadeIcon, icon);
    }

    public void SetSecondaryGun(Sprite icon)
    {
        SetIcon(secondaryWeaponIcon, icon);
    }

    public void SetSecondaryGrenade(Sprite icon)
    {
        SetIcon(secondaryGrenadeIcon, icon);
    }

    public void SetCurrentAmmo(int amount)
    {
        if (currentAmmoTxt)
            currentAmmoTxt.text = amount.ToString();
    }
    public void SetEquippedAmmo(int amount)
    {
        if (equippedAmmoTxt)
            equippedAmmoTxt.text = amount.ToString();
    }
    public void SetEquippedGrenadeAmount(int amount)
    {
        if (equippedGrenadeTxt)
            equippedGrenadeTxt.text = amount.ToString();
    }

    private void SetIcon(Image image, Sprite icon)
    {
        if (image == null || icon == null) return;

        image.sprite = icon;
        image.enabled = icon != null;
    }

}