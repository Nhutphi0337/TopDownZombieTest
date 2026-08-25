using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Game/Attack/Targeting/Area")]
public class AreaTargeting : TargetingStrategy
{
    [SerializeField] private float radius = 3f;

    public override IEnumerable<IDamageable> FindTargets(
        AttackContext context)
    {
        Collider[] colliders =
            Physics.OverlapSphere(
                context.HitPoint,
                radius);

        HashSet<IDamageable> uniqueTargets =
            new HashSet<IDamageable>();

        foreach (Collider collider in colliders)
        {
            IDamageable target =
                collider.GetComponentInParent<IDamageable>();

            if (target == null)
                continue;

            if (uniqueTargets.Add(target))
            {
                yield return target;
            }
        }
    }
}