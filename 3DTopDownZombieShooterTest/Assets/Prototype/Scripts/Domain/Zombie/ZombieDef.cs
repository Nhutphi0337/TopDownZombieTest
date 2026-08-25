using UnityEngine;
[CreateAssetMenu(
    fileName = "ZombieDef",
    menuName = "Game/Zombie/Zombie Definition")]
public class ZombieDef : ScriptableObject
{
    [SerializeField] private string zombieName;
    [SerializeField] private float totalHP;
    [SerializeField] private float moveSpeed;

    [SerializeField] private SoundDef chaseSound;
    [SerializeField] private SoundDef getHitSound;
    [SerializeField] private SoundDef attackSound;
    [SerializeField] private SoundDef deadSound;

    [SerializeField] private ZombieAttackDef[] attacks;
    [SerializeField] private Zombie zombiePrefab;
    [SerializeField] private GameObject deadParticlePrefab;

    public Zombie ZombiePrefab => zombiePrefab;
    public GameObject DeadParticle => deadParticlePrefab;
    public float TotalHP => totalHP;
    public float MoveSpeed => moveSpeed;
    public SoundDef ChaseSound => chaseSound;
    public SoundDef GetHitSound => getHitSound;
    public SoundDef AttackSound => attackSound;
    public SoundDef DeadSound => deadSound;
    public ZombieAttackDef[] Attacks => attacks;
}
