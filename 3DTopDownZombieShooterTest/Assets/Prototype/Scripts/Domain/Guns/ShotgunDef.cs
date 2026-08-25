using UnityEngine;
[CreateAssetMenu(fileName = "ShotgunDef", menuName = "Game/Guns/Shotgun Definition")]
public class ShotGunDef : GunDef
{
    [Header("Shotgun specific")]
    [SerializeField]
    [Min(1)]
    private int pelletCount = 8;

    [SerializeField]
    [Min(0f)]
    private float spreadAngle = 10f;

    public int PelletCount => pelletCount;
    public float SpreadAngle => spreadAngle;
}