using System.Collections.Generic;
using UnityEngine;

public abstract class TargetingStrategy : ScriptableObject
{
    public abstract IEnumerable<IDamageable> FindTargets(
        AttackContext context);
}