using UnityEngine;
[CreateAssetMenu(
    fileName = "PickableItemDef",
    menuName = "Game/Pickable Item Definition")]
public class ItemPickupDef : ScriptableObject
{
    [field: SerializeField] public int amount { get; private set; }
    [field: SerializeField] public ItemType itemType { get; private set; }
    [field: SerializeField] public SoundDef pickSound { get; private set; }
    [field: SerializeField] public PickableDef pickableItem { get; private set; }
    [field: SerializeField] public Pickable prefab { get; private set; }
}
public enum ItemType
{
    Gun,
    Ammo,
    HealthPack,
    RuntimeUpgrade
}

