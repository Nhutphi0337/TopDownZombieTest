using UnityEngine;

[CreateAssetMenu(
    fileName = "AmmoType",
    menuName = "Game/Ammo/AmmoType")]

public class AmmoType : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string displayName;
}
