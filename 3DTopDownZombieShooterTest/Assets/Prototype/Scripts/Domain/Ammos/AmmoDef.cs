using UnityEngine;

[CreateAssetMenu(
    fileName = "Ammo",
    menuName = "Game/Ammo/Ammo Definition")]

public class AmmoDef : PickableDef
{
    [SerializeField]
    private AmmoType type;
    [SerializeField]
    private Bullet bulletPrefab;
    public AmmoType AmmoType => type;
    public Bullet BulletPrefab => bulletPrefab;
}
