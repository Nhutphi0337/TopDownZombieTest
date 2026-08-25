using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Gun")]
    [SerializeField]
    private GunDef gunDef;

    [SerializeField]
    private AmmoDef currentAmmoDef;

    [SerializeField]
    [Min(0)]
    private int currentAmmo;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [Header("Muzzle")]
    [SerializeField]
    private Transform muzzle;

    [Header("Grip References")]
    public Transform leftHandGrip;
    public Transform rightHandGrip;
    public Transform bodyGrip;

    private IPooler pooler;
    private ITeam owner;
    private float nextAllowedFireTime;

    public int CurrentAmmo => currentAmmo;
    public GunDef GunDef => gunDef;
    public AmmoDef CurrentAmmoDef => currentAmmoDef;

    protected Transform Muzzle => muzzle;
    protected ITeam Owner => owner;

    protected virtual void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public virtual void Init(GunDef gunDef, ITeam owner)
    {
        this.gunDef = gunDef;
        this.owner = owner;

        currentAmmo = gunDef.MagazineSize; //1000000000;
        currentAmmoDef = gunDef.DefaultAmmoDef;
    }

    public void SetCurrentAmmoDef(AmmoDef ammoDef)
    {
        currentAmmoDef = ammoDef;
    }

    public void SetPooler(IPooler pooler)
    {
        this.pooler = pooler;
    }

    public virtual bool CanFire()
    {
        return currentAmmo > 0 &&
               Time.time >= nextAllowedFireTime;
    }

    public virtual bool TryFire()
    {
        if (!CanFire() || !IsReadyToFire())
            return false;
        if (!FireProjectiles())
            return false;

        ConsumeAmmo();
        StartFireCooldown();

        return true;
    }

    protected virtual bool FireProjectiles()
    {
        return SpawnBullet(Muzzle.forward);
    }

    protected bool SpawnBullet(Vector3 direction)
    {
        IPoolable pooledObject = pooler.Spawn(
            currentAmmoDef.BulletPrefab.gameObject,
            muzzle.position,
            Quaternion.LookRotation(direction));

        if (pooledObject == null)
            return false;

        Bullet bullet = pooledObject as Bullet;

        if (bullet == null)
        {
            pooler.Return(pooledObject);
            return false;
        }

        bullet.Init(
            owner,
            muzzle.position,
            direction,
            gunDef.AttackDef,
            gunDef.BulletSpeed,
            gunDef.Range);

        return true;
    }

    protected bool IsReadyToFire()
    {
        return muzzle != null &&
               currentAmmoDef != null &&
               currentAmmoDef.BulletPrefab != null &&
               pooler != null;
    }

    protected void ConsumeAmmo(int amount = 1)
    {
        currentAmmo = Mathf.Max(0, currentAmmo - amount);
    }

    protected void StartFireCooldown()
    {
        nextAllowedFireTime = Time.time + gunDef.FireRate;
    }

    public void SpawnMuzzleFlashEffect()
    {
        if (gunDef.MuzzleFlashEffect)
        {
            var vfx = pooler.Spawn(
                gunDef.MuzzleFlashEffect,
                muzzle.position,
                muzzle.rotation) as VisualEffect;

            vfx.transform.SetParent(muzzle);
            vfx.transform.localScale = Vector3.one;
            vfx.transform.localPosition = Vector3.zero;
        }
    }

    public void PlayShootingAnim()
    {
        if (animator)
            animator.SetTrigger("Shoot");
    }

    public void PlayFiringSound()
    {
        if (gunDef.FiringSound)
        {
            AudioManager.Instance.Play(gunDef.FiringSound);
        }
    }

    public bool CanReload()
    {
        return currentAmmo < gunDef.MagazineSize;
    }

    public int GetReloadAmount(int availableAmmo)
    {
        if (availableAmmo <= 0 || currentAmmo >= gunDef.MagazineSize)
            return 0;

        return Mathf.Min(
            gunDef.MagazineSize - currentAmmo,
            availableAmmo);
    }

    public void AddAmmoToMagazine(int amount)
    {
        if (amount <= 0)
            return;

        currentAmmo = Mathf.Min(
            currentAmmo + amount,
            gunDef.MagazineSize);
    }
}