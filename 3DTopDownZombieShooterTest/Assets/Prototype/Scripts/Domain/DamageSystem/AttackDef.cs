using UnityEngine;
[CreateAssetMenu(
    menuName = "Game/Attack/Attack Definition")]
public class AttackDef : ScriptableObject
{
    [SerializeField] private float damage;

    [Header("Targeting")]
    [SerializeField] private TargetingStrategy targeting;

    [SerializeField] private TargetFilter[] filters;

    public void Execute(AttackContext context)
    {
        if (targeting == null)
        {
            Debug.LogError(
                $"{name} has no targeting strategy.",
                this);

            return;
        }

        foreach (IDamageable target in targeting.FindTargets(context))
        {
            if (!PassesFilters(context.Attacker,target))
            {
                continue;
            }
            ApplyDamage(context, target);
        }
    }

    private bool PassesFilters(ITeam attacker, IDamageable target)
    {
        if (filters == null)
            return true;

        foreach (TargetFilter filter in filters)
        {
            if (filter == null)
                continue;

            if (!filter.IsValid(attacker, target))
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyDamage(AttackContext context, IDamageable target)
    {
        DamageInfo damageInfo = new DamageInfo(damage, context.Attacker);
        target.TakeDamage(damageInfo);
    }
}
public struct AttackContext
{
    public ITeam Attacker;
    public Vector3 HitPoint;
    public Collider HitCollider;

    public AttackContext(ITeam attacker, Vector3 hitPoint, Collider hitCollider)
    {
        this.Attacker = attacker;
        this.HitPoint = hitPoint;
        this.HitCollider = hitCollider;
    }
}
