using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Game/Attack/Targeting/Direct")]
public class DirectTargeting : TargetingStrategy
{
    public override IEnumerable<IDamageable> FindTargets(
        AttackContext context)
    {
        if (context.HitCollider == null)
            yield break;

        IDamageable target =
            context.HitCollider
                .GetComponentInParent<IDamageable>();

        if (target != null)
        {
            yield return target;
        }
    }
}