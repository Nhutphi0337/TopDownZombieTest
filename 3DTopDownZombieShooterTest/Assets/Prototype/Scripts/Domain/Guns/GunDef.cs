using UnityEngine;
[CreateAssetMenu(fileName = "GunDef", menuName = "Game/Guns/Gun Definition")]
public class GunDef : PickableDef
{
    [Header("Identity")]
    [SerializeField] protected Gun gunPrefab;
    [SerializeField] protected GunType gunType;
    [SerializeField] protected Sprite icon;

    [Header("Combat")]
    //[SerializeField] private float damage = 25f;
    [SerializeField] protected float range = 20f;
    [SerializeField] protected float bulletSpeed = 30f;
    [SerializeField] protected float fireRate = 5f;
    [SerializeField] protected AttackDef attackDef;

    [Header("Magazine")]
    [SerializeField] protected AmmoDef defaultAmmoDef;
    [SerializeField] protected int magazineSize = 12;
    [SerializeField] protected float reloadTime = 0.5f;

    //[Header("Accuracy")]
    //[SerializeField] protected float spread = 2f;

    [Header("Effects")]
    [field: SerializeField] protected GameObject muzzleFlashEffect;
    [field: SerializeField] protected SoundDef firingSound;

    public Gun GunPrefab => gunPrefab;
    public GunType GunType => gunType;
    public AttackDef AttackDef => attackDef;
    public AmmoDef DefaultAmmoDef => defaultAmmoDef;
    public GameObject MuzzleFlashEffect => muzzleFlashEffect;
    public SoundDef FiringSound => firingSound;
    public Sprite Icon => icon;
    public string GunName => displayName;
    ///public float Damage => damage;
    public float Range => range;
    public float BulletSpeed => bulletSpeed;
    public float FireRate => fireRate;
    public int MagazineSize => magazineSize;
    public float ReloadTime => reloadTime;
}