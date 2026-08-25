using UnityEngine;
using System;
public class PlayerEquipment : MonoBehaviour
{
    private const int SlotCount = 2;

    [SerializeField]
    private Transform[] gunSockets;

    [Header("Start")]
    [SerializeField]
    private GunDef[] startGuns = new GunDef[SlotCount];
    [SerializeField]
    private GrenadeDef[] startGrenades = new GrenadeDef[SlotCount];

    [Header("Gun Slots")]
    [SerializeField]
    private Gun[] guns = new Gun[SlotCount];
    
    [Header("Ammo Storage")]
    [SerializeField]
    private AmmoStorageData[] ammoStorage;

    [Header("Grenade Storage")]
    [SerializeField]
    private GrenadeStorageData[] grenadeStorage;

    private int currentGunIndex;
    private int currentGrenadeIndex;

    private Player owner;
    public Gun CurrentGun
    {
        get
        {
            if (!IsValidGunIndex(currentGunIndex))
            {
                return null;
            }

            return guns[currentGunIndex];
        }
    }
    public GrenadeDef CurrentGrenade
    {
        get
        {
            if (!IsValidGrenadeIndex(currentGrenadeIndex))
            {
                return null;
            }

            var def = grenadeStorage[currentGrenadeIndex].GrenadeDef;

            return  def != null? def: null;
        }
    }
    public int CurrentGunIndex => currentGunIndex;
    public int CurrentGrenadeIndex => currentGrenadeIndex;

    public event Action<Gun, Gun, int> OnCurrentGunActivate;
    public event Action<GrenadeDef, GrenadeDef, int> OnCurrentGrenadeActivate;
    
    public event Action<int> OnCurrentAmmoAmountChanged;
    public event Action<int> OnCurrentGrenadeAmountChanged;

    private void Awake()
    {
        EnsureSlotArrays();
    }
    public void Init(Player owner)
    {
        this.owner = owner;
        
        //Start guns
        if (startGuns.Length != 0)
        {
            for (int i = 0; i < 2; i++)
            {
                PickNewGun(startGuns[i], i);
            }
            SetCurrentGunActive();
        }

        //Start grenades
        if (startGrenades.Length != 0)
        {
            for (int i = 0; i < 2; i++)
            {
                PickNewGrenade(startGrenades[i], i);
            }
            SetCurrentGrenadeActive();
        }
    }
    public bool HaveGun(GunDef gunDef)
    {
        foreach(var gun in guns)
        {
            if (gun.GunDef == gunDef) return true;
        }

        return false;
    }
    public bool PickNewGun(GunDef gunDef, int slot)
    {
        if (CurrentGun != null && CurrentGun.GunDef == gunDef)
            return false;

        var socket = gunSockets[0];
        if (gunDef.GunType.DisplayName == "Rifle")
            socket = gunSockets[1];
        else if (gunDef.GunType.DisplayName == "Shotgun")
            socket = gunSockets[2];

        if (guns[slot] != null)
            Destroy(guns[slot].gameObject);

        var gun = Instantiate(gunDef.GunPrefab, socket);
        gun.transform.localPosition = Vector3.zero;
        gun.gameObject.SetActive(false);
        
        gun.Init(gunDef, owner);
        gun.SetPooler(owner.Pooler);

        guns[slot] = gun;

        ammoStorage[slot].SetAmmoDef(gun.CurrentAmmoDef);
        ammoStorage[slot].ResetAmount();
        ammoStorage[slot].Add(gunDef.MagazineSize*3);

        return true;
    }
    public bool PickNewGrenade(GrenadeDef grenadeDef, int slot)
    {
        if (CurrentGrenade != null && CurrentGrenade == grenadeDef)
            return false;

        grenadeStorage[slot].SetGrenadeDef(grenadeDef);
        grenadeStorage[slot].ResetAmount();
        grenadeStorage[slot].Add(grenadeDef.BaseAmount);

        return true;
    }

    public void EquipNewAmmo(Gun gun)
    {
        //foreach(var slot in ammoStorage)
        //{
        //    if (slot.AmmoDef.AmmoType == gun.GunDef.DefaultAmmoDef.AmmoType)
        //        return;
        //}

        
    }
    public bool EquipGun(Gun gun, int slot)
    {
        if (gun == null || !IsValidGunIndex(slot))
        {
            return false;
        }

        guns[slot] = gun;
        SetCurrentGunActive();

        return true;
    }
    public bool EquipCurrentGun(int slot)
    {
        if (!IsValidGunIndex(slot))
        {
            return false;
        }

        if (guns[slot] == null)
        {
            return false;
        }

        currentGunIndex = slot;

        SetCurrentGunActive();

        return true;
    }

    public bool EquipGrenade(GrenadeDef grenade, int slot)
    {
        if (grenade == null || !IsValidGrenadeIndex(slot))
        {
            return false;
        }

        grenadeStorage[slot].SetGrenadeDef(grenade);

        return true;
    }

    public bool SwitchGun()
    {
        if (guns.Length < SlotCount)
        {
            return false;
        }

        int nextIndex = currentGunIndex == 0 ? 1 : 0;

        if (guns[nextIndex] == null)
        {
            return false;
        }

        currentGunIndex = nextIndex;
        SetCurrentGunActive();
        return true;
    }

    public bool SwitchGrenade()
    {
        if (grenadeStorage.Length < SlotCount)
        {
            return false;
        }

        int nextIndex = currentGrenadeIndex == 0 ? 1 : 0;

        if (grenadeStorage[nextIndex].GrenadeDef == null)
        {
            return false;
        }

        currentGrenadeIndex = nextIndex;        
        SetCurrentGrenadeActive();
        
        return true;
    }

    public int GetAmmoAmount(
        AmmoDef ammoDef)
    {
        AmmoStorageData storage =
            FindAmmoStorage(ammoDef);

        if (storage == null)
        {
            return 0;
        }

        return storage.Amount;
    }

    public bool HasAmmo(
        AmmoDef ammoDef,
        int amount = 1)
    {
        AmmoStorageData storage =
            FindAmmoStorage(ammoDef);

        return storage != null &&
               storage.CanConsume(amount);
    }

    public bool ConsumeAmmo(
        AmmoDef ammoDef,
        int amount = 1)
    {
        AmmoStorageData storage =
            FindAmmoStorage(ammoDef);

        if (storage == null)
        {
            return false;
        }
        
        var consume = storage.Consume(amount);

        if (ammoDef == ammoStorage[currentGunIndex].AmmoDef)
            OnCurrentAmmoAmountChanged?.Invoke(storage.Amount);

        return consume;
    }

    public bool AddAmmo(
        AmmoDef ammoDef,
        int amount)
    {
        if (ammoDef == null ||
            amount <= 0)
        {
            return false;
        }

        AmmoStorageData storage =
            FindAmmoStorage(ammoDef);

        if (storage != null)
        {
            storage.Add(amount);
            if (ammoDef == ammoStorage[currentGunIndex].AmmoDef)
                OnCurrentAmmoAmountChanged?.Invoke(storage.Amount);
            return true;
        }

        return false;
    }

    public int GetGrenadeAmount(
        GrenadeDef grenadeDef)
    {
        GrenadeStorageData storage =
            FindGrenadeStorage(grenadeDef);

        if (storage == null)
        {
            return 0;
        }

        return storage.Amount;
    }

    public bool HasGrenade(
        GrenadeDef grenadeDef,
        int amount = 1)
    {
        GrenadeStorageData storage =
            FindGrenadeStorage(grenadeDef);

        return storage != null &&
               storage.CanConsume(amount);
    }

    public bool ConsumeGrenade(
        GrenadeDef grenadeDef,
        int amount = 1)
    {
        GrenadeStorageData storage =
            FindGrenadeStorage(grenadeDef);

        if (storage == null)
        {
            return false;
        }

        var consume = storage.Consume(amount);

        if (grenadeDef == CurrentGrenade)
            OnCurrentGrenadeAmountChanged?.Invoke(storage.Amount);

        return consume;
    }

    public void AddGrenades(
        GrenadeDef grenadeDef,
        int amount)
    {
        if (grenadeDef == null ||
            amount <= 0)
        {
            return;
        }

        GrenadeStorageData storage =
            FindGrenadeStorage(grenadeDef);

        if (storage != null)
        {
            storage.Add(amount);
            if (grenadeDef == CurrentGrenade)
                OnCurrentGrenadeAmountChanged?.Invoke(storage.Amount);
        }
    }

    private AmmoStorageData FindAmmoStorage(
        AmmoDef ammoDef)
    {
        if (ammoDef == null ||
            ammoStorage == null)
        {
            return null;
        }

        for (int i = 0;
             i < ammoStorage.Length;
             i++)
        {
            AmmoStorageData storage =
                ammoStorage[i];

            if (storage == null)
            {
                continue;
            }

            if (storage.AmmoDef == ammoDef)
            {
                return storage;
            }
        }

        return null;
    }

    private GrenadeStorageData FindGrenadeStorage(
        GrenadeDef grenadeDef)
    {
        if (grenadeDef == null ||
            grenadeStorage == null)
        {
            return null;
        }

        for (int i = 0;
             i < grenadeStorage.Length;
             i++)
        {
            GrenadeStorageData storage =
                grenadeStorage[i];

            if (storage == null)
            {
                continue;
            }

            if (storage.GrenadeDef == grenadeDef)
            {
                return storage;
            }
        }

        return null;
    }

    private void EnsureSlotArrays()
    {
        if (guns == null ||
            guns.Length != SlotCount)
        {
            Gun[] existingGuns = guns;

            guns = new Gun[SlotCount];

            if (existingGuns != null)
            {
                int count =
                    Mathf.Min(
                        existingGuns.Length,
                        SlotCount);

                for (int i = 0; i < count; i++)
                {
                    guns[i] = existingGuns[i];
                }
            }
        }

        if (grenadeStorage == null ||
            grenadeStorage.Length != SlotCount)
        {
            GrenadeStorageData[] existingGrenades = grenadeStorage;

            grenadeStorage = new GrenadeStorageData[SlotCount];

            if (existingGrenades != null)
            {
                int count = Mathf.Min(existingGrenades.Length, SlotCount);

                for (int i = 0; i < count; i++)
                {
                    grenadeStorage[i] = existingGrenades[i];
                }
            }
        }
    }
    private void SetCurrentGunActive()
    {
        foreach(var gun in guns)
        {
            gun.gameObject.SetActive(false);
        }

        var curGun = guns[currentGunIndex];
        curGun.gameObject.SetActive(true);

        var secondGun = guns[currentGunIndex == 0 ? 1 : 0];

        OnCurrentGunActivate?.Invoke(curGun, secondGun, GetAmmoAmount(curGun.CurrentAmmoDef));
    }
    private void SetCurrentGrenadeActive()
    {
        var curGrenade = CurrentGrenade;
        var secondGrenade = grenadeStorage[currentGrenadeIndex == 0 ? 1 : 0].GrenadeDef;

        OnCurrentGrenadeActivate?.Invoke(curGrenade, secondGrenade, GetGrenadeAmount(curGrenade));
    }

    private bool IsValidGunIndex(int index)
    {
        return guns != null &&
               index >= 0 &&
               index < guns.Length;
    }

    private bool IsValidGrenadeIndex(int index)
    {
        return grenadeStorage != null &&
               index >= 0 &&
               index < grenadeStorage.Length;
    }
}