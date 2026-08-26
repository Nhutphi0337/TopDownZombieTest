using System;
using UnityEngine;
public class Player : MonoBehaviour, IDamageable, ITeam
{
    [field:SerializeField] public float maxHP { get; private set; }
    [field:SerializeField] public float currentHP { get; private set; }
    public IPooler Pooler { private set; get; }

    public HitFlash HitFlash { private set; get; } //For testing
    public PlayerInput Input { private set; get; }
    public PlayerAim Aim { private set; get; }
    public PlayerMovement Movement { private set; get; }
    public PlayerPicking Picking { private set; get; }
    public PlayerEquipment Equipment { private set; get; }
    public PlayerAnimationController AnimationController { private set; get; }
    public PlayerGunController GunController { private set; get; }
    public PlayerGunGripController GunGripController { private set; get; }
    public PlayerGrenadeController GrenadeController { private set; get; }
    public Team Team => Team.Player;

    public bool isAlive { get; private set; }

    public event Action<float/*current HP*/, float/*previous HP*/, float/*max HP*/> OnHeal;
    public event Action<float/*current HP*/, float/*previous HP*/, float/*max HP*/> OnTakeDamage;

    public event Action OnDead;
    private void Awake()
    {
        Input = GetComponent<PlayerInput>();
        Aim = GetComponent<PlayerAim>();
        HitFlash = GetComponent<HitFlash>();
        Movement = GetComponent<PlayerMovement>();
        AnimationController = GetComponent<PlayerAnimationController>();
        Picking = GetComponent<PlayerPicking>();
        Equipment = GetComponent<PlayerEquipment>();
        GunController = GetComponent<PlayerGunController>();
        GunGripController = GetComponent<PlayerGunGripController>();
        GrenadeController = GetComponent<PlayerGrenadeController>();
        
        Pooler = FindObjectOfType<Pooler>();
    }

    public void Init()
    {
        isAlive = true;
        Movement.Init(this);
        Aim.Init(this);
        Picking.Init(this);
        Equipment.Init(this);
        GunController.Init(this);
        GrenadeController.Init(this);
    }

    void Update()
    {
        
    }

    public void Die()
    {
        isAlive = false;

        if(Input != null)
        {
            Input.DisableActions();
        }

        if(GunController != null)
        {
            GunController.SetActiveCurrentGun(false);
        }
        if(GunGripController != null)
        {
            GunGripController.DisableFollowing();
        }
        if (GrenadeController != null)
        {
            GrenadeController.CancelThrow();
        }
        if (Movement != null)
        {
            Movement.SetMovementEnabled(false);
        }
        AnimationController.PlayDeath();

        OnDead?.Invoke();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        var prevHp = currentHP;
        currentHP -= damageInfo.Amount;

        if (HitFlash != null)
            HitFlash.Play();

        OnTakeDamage?.Invoke(currentHP, prevHp, maxHP);

        if (currentHP <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        var prevHp = currentHP;
        currentHP += amount;

        OnHeal?.Invoke(currentHP, prevHp, maxHP);
    }
}
