using UnityEngine;

[CreateAssetMenu(
    menuName = "Game/Attack/Filters/Enemy")]
public class EnemyFilter : TargetFilter
{
    public override bool IsValid(
        ITeam attacker,
        IDamageable target)
    {
        if (attacker == null || target == null)
            return false;
                   
        ITeam targetTeam =
            (target as Component)?
            .GetComponentInParent<ITeam>();

        if (attacker == null ||
            targetTeam == null)
        {
            return false;
        }

        return attacker.Team != targetTeam.Team;
    }
}