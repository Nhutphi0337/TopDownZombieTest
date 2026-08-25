using UnityEngine;
public class PlayerGrenadeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SoundDef switchSound;
    [SerializeField] private Transform throwOrigin;

    private Player player;

    [Header("Throw")]
    [SerializeField, Min(0f)] private float minThrowForce = 5f;
    [SerializeField, Min(0f)] private float maxThrowForce = 12f;
    [SerializeField, Min(0f)] private float maxThrowDistance = 10f;
    [SerializeField, Min(0.01f)] private float maxHoldTime = 1f;
    [SerializeField, Min(0f)] private float throwPointTime = 0.2f;
    [SerializeField, Min(0f)] private float throwInterval = 0.75f;
    [SerializeField, Min(0f)] private float gunGripRestoreTime = 0.5f;

    private bool isCharging;
    private bool isThrowing;

    private float holdTime;
    private float throwTimer;
    private float throwCooldown;
    private float gunGripRestoreTimer;

    private Vector3 throwDirection;

    private GrenadeDef pendingGrenade;
    private float pendingThrowForce;

    public bool IsCharging => isCharging;

    public int GrenadeCount
    {
        get
        {
            GrenadeDef grenade = GetCurrentGrenade();

            if (grenade == null || player.Equipment == null)
                return 0;

            return player.Equipment.GetGrenadeAmount(grenade);
        }
    }

    public void Init(Player owner)
    {
        player = owner;
    }

    private void Update()
    {
        if (player == null)
            return;

        UpdateCooldown();
        UpdateGunGripRestore();

        if (isThrowing)
        {
            UpdatePendingThrow();
            return;
        }

        HandleInput();

        if (isCharging)
            UpdateCharging();
    }

    private void UpdateCooldown()
    {
        if (throwCooldown > 0f)
            throwCooldown -= Time.deltaTime;
    }

    private void UpdateGunGripRestore()
    {
        if (gunGripRestoreTimer <= 0f)
            return;

        gunGripRestoreTimer -= Time.deltaTime;

        if (gunGripRestoreTimer <= 0f)
        {
            player.GunController.SetActiveCurrentGun(true);
            player.GunGripController.EnableFollowing();
        }
    }

    private void UpdatePendingThrow()
    {
        throwTimer -= Time.deltaTime;

        if (throwTimer <= 0f)
            SpawnGrenade();
    }

    private void HandleInput()
    {
        if (player.Input.ThrowGrenadePressedThisFrame)
            StartThrow(transform.forward);

        if (player.Input.ThrowGrenadeReleasedThisFrame)
            ReleaseThrow();

        if (player.Input.SwitchGrenadePressedThisFrame)
            SwitchGrenade();
    }

    private void UpdateCharging()
    {
        holdTime += Time.deltaTime;

        if (holdTime < maxHoldTime)
            return;

        holdTime = maxHoldTime;
        Throw();
    }

    public bool StartThrow(Vector3 direction)
    {
        if (isCharging || isThrowing || throwCooldown > 0f)
            return false;

        GrenadeDef grenade = GetCurrentGrenade();

        if (grenade == null || player.Equipment == null || !player.Equipment.HasGrenade(grenade))
            return false;

        direction = Vector3.ProjectOnPlane(direction, Vector3.up);

        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        throwDirection = direction.normalized;
        holdTime = 0f;
        isCharging = true;

        return true;
    }

    public bool ReleaseThrow()
    {
        return isCharging && Throw();
    }

    public bool SwitchGrenade()
    {
        CancelThrow();

        if (player.Equipment == null)
            return false;

        if (!player.Equipment.SwitchGrenade())
            return false;

        AudioManager.Instance.Play(switchSound);

        return true;
    }

    public bool EquipGrenade(GrenadeDef grenade, int slot)
    {
        if (player.Equipment == null)
            return false;

        return player.Equipment.EquipGrenade(grenade, slot);
    }

    public void CancelThrow()
    {
        isCharging = false;
        isThrowing = false;

        holdTime = 0f;
        throwTimer = 0f;
        gunGripRestoreTimer = 0f;

        throwDirection = Vector3.zero;

        pendingGrenade = null;
        pendingThrowForce = 0f;
    }

    private bool Throw()
    {
        GrenadeDef grenade = GetCurrentGrenade();

        if (!CanThrow(grenade))
        {
            CancelThrow();
            return false;
        }

        float charge = Mathf.Clamp01(holdTime / maxHoldTime);

        pendingGrenade = grenade;
        pendingThrowForce = Mathf.Lerp(minThrowForce, maxThrowForce, charge);

        isCharging = false;
        isThrowing = true;
        throwTimer = throwPointTime;

        player.GunController.SetActiveCurrentGun(false);
        player.GunGripController.DisableFollowing();
        player.AnimationController.PlayThrowGrenade();

        gunGripRestoreTimer = gunGripRestoreTime;

        if (throwPointTime <= 0f)
            SpawnGrenade();

        return true;
    }

    private void SpawnGrenade()
    {
        if (!isThrowing)
            return;

        GrenadeDef grenadeDef = pendingGrenade;

        if (!CanThrow(grenadeDef))
        {
            CancelThrow();
            return;
        }

        Grenade grenadePrefab = grenadeDef.GrenadePrefab;

        if (grenadePrefab == null)
        {
            CancelThrow();
            return;
        }

        IPoolable pooledObject = player.Pooler.Spawn(
            grenadePrefab.gameObject,
            throwOrigin.position,
            Quaternion.identity);

        if (pooledObject is not Grenade grenade)
        {
            if (pooledObject != null)
                player.Pooler.Return(pooledObject);

            CancelThrow();
            return;
        }

        if (!player.Equipment.ConsumeGrenade(grenadeDef))
        {
            player.Pooler.Return(grenade);
            CancelThrow();
            return;
        }

        grenade.Initialize(
            player,
            grenadeDef,
            throwDirection,
            pendingThrowForce,
            maxThrowDistance,
            grenadeDef.FuseTime);

        throwCooldown = throwInterval;

        ClearPendingThrow();
    }

    private bool CanThrow(GrenadeDef grenade)
    {
        return grenade != null &&
               player.Equipment != null &&
               player.Pooler != null &&
               throwOrigin != null &&
               player.Equipment.HasGrenade(grenade);
    }

    private void ClearPendingThrow()
    {
        isThrowing = false;
        throwTimer = 0f;
        throwDirection = Vector3.zero;
        pendingGrenade = null;
        pendingThrowForce = 0f;
        holdTime = 0f;
    }

    private GrenadeDef GetCurrentGrenade()
    {
        return player.Equipment?.CurrentGrenade;
    }
}