using System;
using UnityEngine;

public class Zombie : MonoBehaviour, IPoolable, IDamageable, ITeam
{
    [Header("Configuration")]
    [SerializeField] private ZombieDef zombieDef;

    [Header("AI")]
    [SerializeField, Min(0.01f)] private float thinkInterval = 0.1f;

    [Header("Components")]
    [SerializeField] private ZombieAnimationController animationController;
    [SerializeField] private HitFlash hitflash;
    [SerializeField] private ZombieMovement movement;
    [SerializeField] private ZombieAttackController attack;

    [Header("Runtime Data")]
    [SerializeField] private float currentHP;

    private bool isAlive;
    private float thinkTimer;

    public IPooler pooler { get; private set; }

    private StateMachine stateMachine;
    private ZombieChaseState chaseState;
    private ZombieAttackState attackState;
    private ZombieDeadState deadState;

    public ZombieDef ZombieDef => zombieDef;
    public ZombieAnimationController AnimationController => animationController;
    public ZombieMovement Movement => movement;
    public ZombieAttackController Attack => attack;

    public Transform Target =>
        FlowFieldManager.Instance != null
            ? FlowFieldManager.Instance.Target
            : null;

    public StateMachine StateMachine => stateMachine;

    public Team Team => Team.Enemy;

    public event Action<Zombie> OnDead;

    private void Awake()
    {
        animationController = GetComponent<ZombieAnimationController>();
        movement = GetComponent<ZombieMovement>();
        attack = GetComponent<ZombieAttackController>();
        hitflash = GetComponent<HitFlash>();

        CreateStateMachine();
    }

    private void Update()
    {
        if (!isAlive)
            return;

        UpdateAI();
        attack.UpdateAttack(Time.deltaTime);
    }

    private void UpdateAI()
    {
        thinkTimer -= Time.deltaTime;

        if (thinkTimer > 0f)
            return;

        thinkTimer = thinkInterval;
        stateMachine.Tick();
    }

    private void CreateStateMachine()
    {
        stateMachine = new StateMachine();

        chaseState = new ZombieChaseState(this);
        attackState = new ZombieAttackState(this);
        deadState = new ZombieDeadState(this);

        stateMachine.AddState(chaseState);
        stateMachine.AddState(attackState);
        stateMachine.AddState(deadState);

        stateMachine.AddTransition<ZombieChaseState, ZombieAttackState>(
            attackState,
            () => Attack.HasAvailableAttack);

        stateMachine.AddTransition<ZombieAttackState, ZombieChaseState>(
            chaseState,
            () => !Attack.IsBusy && !Attack.HasAvailableAttack);

        stateMachine.AddTransition<ZombieChaseState, ZombieDeadState>(
            deadState,
            () => currentHP <= 0);

        stateMachine.AddTransition<ZombieAttackState, ZombieDeadState>(
            deadState,
            () => currentHP <= 0);
    }

    public void Init(ZombieDef def)
    {
        zombieDef = def;

        movement.SetMoveSpeed(zombieDef.MoveSpeed);
        attack.Init(this);

        ResetZombieState();
    }

    public void SetPooler(IPooler pooler)
    {
        this.pooler = pooler;
    }

    public void Die()
    {
        isAlive = false;

        OnDead?.Invoke(this);

        pooler.Spawn(
            zombieDef.DeadParticle,
            transform.position,
            Quaternion.identity);

        pooler.Return(this);
    }

    public void ResetZombieState()
    {
        isAlive = true;
        thinkTimer = 0f;

        animationController.ResetAnimStates();

        stateMachine.SetInitialState<ZombieChaseState>();

        currentHP = zombieDef.TotalHP;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        currentHP -= damageInfo.Amount;

        AudioManager.Instance.Play(zombieDef.GetHitSound);
        hitflash.Play();
    }

    public void OnSpawned()
    {
    }

    public void OnReleased()
    {
        hitflash.ResetFlash();
    }
}