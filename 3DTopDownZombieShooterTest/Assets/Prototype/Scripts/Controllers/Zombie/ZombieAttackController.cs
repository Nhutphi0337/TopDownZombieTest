using System.Collections.Generic;
using UnityEngine;

public class ZombieAttackController : MonoBehaviour
{
    private Zombie zombie;

    private ZombieDef zombieDef;
    private ZombieAttackDef currentAttack;

    private float timer;
    private bool attackTriggered;

    public bool IsAttacking { get; private set; }

    public bool IsOnCooldown { get; private set; }

    public ZombieAttackDef CurrentAttackDef => currentAttack;

    /// <summary>
    /// Returns true if at least one attack can currently be used
    /// against the target.
    /// </summary>
    public bool HasAvailableAttack
    {
        get
        {
            return HasAttackInRange();
        }
    }

    /// <summary>
    /// True while the current attack or its cooldown is still running.
    /// The zombie should not leave the Attack state during this time.
    /// </summary>
    public bool IsBusy =>
        IsAttacking || IsOnCooldown;

    public void Awake()
    {
    }

    public void Init(Zombie owner)
    {
        zombie = owner;
        zombieDef = zombie.ZombieDef;
    }

    public void StartAttack()
    {
        if (zombieDef == null)
        {
            Debug.LogError(
                $"{name}: ZombieAttack has not been initialized.",
                this);

            return;
        }

        if (IsAttacking || IsOnCooldown)
            return;

        currentAttack =
            SelectRandomAvailableAttack();

        if (currentAttack == null)
            return;

        timer = 0f;
        attackTriggered = false;

        IsAttacking = true;

        if (zombie.Target != null)
        {
            zombie.Movement.FaceTarget(
                zombie.Target.position);
        }
    }

    public void UpdateAttack(float deltaTime)
    {
        if (zombieDef == null)
            return;

        if (IsOnCooldown)
        {
            UpdateCooldown(deltaTime);
            return;
        }

        if (!IsAttacking ||
            currentAttack == null)
        {
            return;
        }

        timer += deltaTime;

        if (!attackTriggered &&
            timer >= currentAttack.AttackPoint)
        {
            attackTriggered = true;

            if (IsTargetInAttackRange(currentAttack))
            {
                PerformAttack(currentAttack);
            }
        }

        if (timer >= currentAttack.TotalAttackTime)
        {
            FinishAttack();
        }
    }

    private void FinishAttack()
    {
        IsAttacking = false;
        IsOnCooldown = true;

        timer = 0f;
    }

    private void UpdateCooldown(float deltaTime)
    {
        timer += deltaTime;

        if (currentAttack == null)
        {
            timer = 0f;
            IsOnCooldown = false;
            return;
        }

        if (timer >= currentAttack.Cooldown)
        {
            timer = 0f;

            IsOnCooldown = false;
            currentAttack = null;
        }
    }

    private bool HasAttackInRange()
    {
        if (zombieDef == null ||
            zombieDef.Attacks == null ||
            zombieDef.Attacks.Length == 0)
        {
            return false;
        }

        if (zombie == null ||
            zombie.Target == null)
        {
            return false;
        }

        float distanceSqr =
            GetHorizontalDistanceSqr(
                transform.position,
                zombie.Target.position);

        for (int i = 0;
             i < zombieDef.Attacks.Length;
             i++)
        {
            ZombieAttackDef attack =
                zombieDef.Attacks[i];

            if (attack == null)
                continue;

            float rangeSqr =
                attack.Range *
                attack.Range;

            if (distanceSqr <= rangeSqr)
                return true;
        }

        return false;
    }

    private bool IsTargetInAttackRange(
        ZombieAttackDef attack)
    {
        if (zombie == null ||
            zombie.Target == null ||
            attack == null)
        {
            return false;
        }

        float distanceSqr =
            GetHorizontalDistanceSqr(
                transform.position,
                zombie.Target.position);

        float rangeSqr =
            attack.Range *
            attack.Range;

        return distanceSqr <= rangeSqr;
    }

    private ZombieAttackDef SelectRandomAvailableAttack()
    {
        if (zombieDef == null ||
            zombieDef.Attacks == null ||
            zombieDef.Attacks.Length == 0)
        {
            return null;
        }

        if (zombie == null ||
            zombie.Target == null)
        {
            return null;
        }

        float distanceSqr =
            GetHorizontalDistanceSqr(
                transform.position,
                zombie.Target.position);

        List<ZombieAttackDef> availableAttacks =
            new List<ZombieAttackDef>();

        for (int i = 0;
             i < zombieDef.Attacks.Length;
             i++)
        {
            ZombieAttackDef attack =
                zombieDef.Attacks[i];

            if (attack == null)
                continue;

            float rangeSqr =
                attack.Range *
                attack.Range;

            if (distanceSqr <= rangeSqr)
            {
                availableAttacks.Add(attack);
            }
        }

        if (availableAttacks.Count == 0)
            return null;

        int randomIndex =
            Random.Range(
                0,
                availableAttacks.Count);

        return availableAttacks[randomIndex];
    }

    private void PerformAttack(
        ZombieAttackDef attack)
    {
        if (attack.AttackDef == null)
            return;

        if (!zombie.Target.TryGetComponent<IDamageable>(
                out IDamageable damageable))
        {
            return;
        }

        attack.AttackDef.Execute(
            new AttackContext(
                zombie,
                zombie.Target.transform.position,
                zombie.Target.GetComponent<Collider>()));
    }

    private float GetHorizontalDistanceSqr(
        Vector3 a,
        Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return (a - b).sqrMagnitude;
    }
}