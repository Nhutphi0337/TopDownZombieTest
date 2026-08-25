using System;
using System.Collections;
using UnityEngine;

public class PlayerGunController : MonoBehaviour
{
    [SerializeField] private SoundDef reloadSound;

    private Player player;
    private Coroutine reloadCoroutine;

    public event Action<Gun> OnShoot;
    public event Action<Gun, int> OnReloadDone;

    public bool IsReloading => reloadCoroutine != null;

    public void Init(Player owner)
    {
        player = owner;
        ApplyCurrentGun();
    }

    private void Update()
    {
        if (player == null)
            return;

        if (player.Input.ShootPressedThisFrame)
            TryShoot();

        if (player.Input.ReloadPressedThisFrame)
            Reload();

        if (player.Input.SwitchGunPressedThisFrame)
            SwitchGun();
    }

    public void SetActiveCurrentGun(bool active)
    {
        Gun currentGun = GetCurrentGun();

        if (currentGun != null)
            currentGun.gameObject.SetActive(active);
    }

    public Gun GetCurrentGun()
    {
        if (player.Equipment == null)
            return null;

        return player.Equipment.CurrentGun;
    }

    public GunType GetCurrentGunType()
    {
        Gun gun = GetCurrentGun();

        if (gun == null)
            return null;

        return gun.GunDef.GunType;
    }

    public int GetCurrentAmmo()
    {
        Gun gun = GetCurrentGun();

        if (gun == null)
            return 0;

        return gun.CurrentAmmo;
    }

    public int GetCurrentMagazineCapacity()
    {
        Gun gun = GetCurrentGun();

        if (gun == null)
            return 0;

        return gun.GunDef.MagazineSize;
    }

    public int GetCurrentReserveAmmo()
    {
        Gun gun = GetCurrentGun();

        if (gun == null ||
            player.Equipment == null ||
            gun.CurrentAmmoDef == null)
        {
            return 0;
        }

        return player.Equipment.GetAmmoAmount(
            gun.CurrentAmmoDef);
    }

    public bool TryShoot()
    {
        if (IsReloading)
            return false;

        Gun gun = GetCurrentGun();

        if (gun == null)
            return false;

        if (!gun.TryFire())
        {
            if(gun.CurrentAmmo <= 0)
                Reload();
            return false;
        }

        if (player.AnimationController != null)
            player.AnimationController.PlayShoot();

        gun.PlayFiringSound();
        gun.PlayShootingAnim();
        gun.SpawnMuzzleFlashEffect();

        OnShoot?.Invoke(gun);

        return true;
    }

    public bool Reload()
    {
        if (IsReloading)
            return false;

        Gun gun = GetCurrentGun();

        if (gun == null ||
            player.Equipment == null ||
            gun.CurrentAmmoDef == null)
        {
            return false;
        }

        if (!gun.CanReload())
            return false;

        int availableAmmo = player.Equipment.GetAmmoAmount(
            gun.CurrentAmmoDef);

        int reloadAmount = gun.GetReloadAmount(
            availableAmmo);

        if (reloadAmount <= 0)
            return false;

        reloadCoroutine = StartCoroutine(
            ReloadRoutine(gun, reloadAmount));

        return true;
    }

    private IEnumerator ReloadRoutine(Gun gun, int reloadAmount)
    {
        yield return new WaitForSeconds(
            gun.GunDef.ReloadTime);

        if (player == null ||
            player.Equipment == null ||
            gun == null ||
            gun.CurrentAmmoDef == null)
        {
            reloadCoroutine = null;
            yield break;
        }

        if (player.Equipment.ConsumeAmmo(gun.CurrentAmmoDef, reloadAmount))
        {
            gun.AddAmmoToMagazine(reloadAmount);

            AudioManager.Instance.Play(reloadSound);

            OnReloadDone?.Invoke(gun, GetCurrentReserveAmmo());
        }

        reloadCoroutine = null;
    }

    public void CancelReload()
    {
        if (reloadCoroutine == null)
            return;

        StopCoroutine(reloadCoroutine);
        reloadCoroutine = null;
    }

    public bool SwitchGun()
    {
        if (player.Equipment == null)
            return false;

        CancelReload();

        if (!player.Equipment.SwitchGun())
            return false;

        ApplyCurrentGun();

        AudioManager.Instance.Play(reloadSound);

        return true;
    }

    public bool EquipGun(Gun gun, int slot)
    {
        if (player.Equipment == null)
            return false;

        if (!player.Equipment.EquipGun(gun, slot))
            return false;

        if (player.Equipment.CurrentGunIndex == slot)
            ApplyCurrentGun();

        return true;
    }

    public bool EquipCurrentGun(int slot)
    {
        if (player.Equipment == null)
            return false;

        CancelReload();

        if (!player.Equipment.EquipCurrentGun(slot))
            return false;

        ApplyCurrentGun();

        return true;
    }

    private void ApplyCurrentGun()
    {
        Gun gun = GetCurrentGun();

        if (gun == null)
        {
            if (player.AnimationController != null)
                player.AnimationController.SetGunType(null);

            player.GunGripController?.ClearGun();

            return;
        }

        if (player.AnimationController != null)
        {
            player.AnimationController.SetGunType(
                gun.GunDef.GunType);
        }

        player.GunGripController?.SetGun(gun);
    }
}