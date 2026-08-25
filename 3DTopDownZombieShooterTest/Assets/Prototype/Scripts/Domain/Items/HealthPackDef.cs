using UnityEngine;
[CreateAssetMenu(
    fileName = "HealthPackDef",
    menuName = "Game/Health Pack Definition")]
public class HealthPackDef : PickableDef
{
    [field: SerializeField] public float healAmount { get; private set; }
}
