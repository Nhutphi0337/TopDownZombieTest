using UnityEngine;
[CreateAssetMenu(
    fileName = "ZombieAttackDef",
    menuName = "Game/Zombie/Zombie Attack")]
public class ZombieAttackDef : ScriptableObject
{
    [Min(0f)]
    [SerializeField] private float range = 1.2f;

    [Min(0.01f)]
    [SerializeField] private float totalAttackTime = 0.8f;

    [Min(0f)]
    [SerializeField] private float attackPoint = 0.35f;

    [Min(0f)]
    [SerializeField] private float cooldown = 1f;

    [SerializeField] private AttackDef attackDef;

    [Header("Animation")]
    [SerializeField]
    private AnimatorOverrideController animationOverrideController;
    public AnimatorOverrideController AnimationOverrideController =>
        animationOverrideController;

    public AttackDef AttackDef => attackDef;
    public float Range =>
        range;

    public float TotalAttackTime =>
        totalAttackTime;

    public float AttackPoint =>
        attackPoint;

    public float Cooldown =>
        cooldown;
    public void Validate()
    {
        totalAttackTime =
            Mathf.Max(0.01f, totalAttackTime);

        attackPoint =
            Mathf.Clamp(
                attackPoint,
                0f,
                totalAttackTime);

        cooldown =
            Mathf.Max(0f, cooldown);

        range =
            Mathf.Max(0f, range);
    }
}