using UnityEngine;
public abstract class TargetFilter : ScriptableObject
{
    public abstract bool IsValid(
        ITeam attacker,
        IDamageable target);
}