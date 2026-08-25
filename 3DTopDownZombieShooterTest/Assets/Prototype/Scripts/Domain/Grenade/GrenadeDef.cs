using System.Collections;
using System.Threading;
using UnityEngine;
[CreateAssetMenu(
    fileName = "GrenadeDef",
    menuName = "Game/Grenade/Grenade Definition")]
public class GrenadeDef : PickableDef
{
    [field: SerializeField] public Grenade GrenadePrefab { get; private set; }
    
    [SerializeField] private Sprite icon;

    [Header("Amount")]
    [SerializeField] private int baseAmount = 10;
    [SerializeField] private int maxAmount = 20;

    [Header("Attack")]
    [SerializeField] private AttackDef attackDef;
    [SerializeField] private float fuseTime;

    [Header("Effects")]
    [SerializeField] private GameObject explosionVfx;
    [SerializeField] private SoundDef explosionSound;

    public int BaseAmount => baseAmount;
    public int MaxAmount => maxAmount;
    public float FuseTime => fuseTime;

    public Sprite Icon => icon;
    public AttackDef AttackDef => attackDef;
    public GameObject ExplosionVfx => explosionVfx;
    public SoundDef ExplosionSound => explosionSound;
}
