using UnityEngine;

[CreateAssetMenu(
    fileName = "GunType",
    menuName = "Game/Guns/Gun Type")]
public class GunType : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string displayName;

    [Header("Animation")]
    [SerializeField]
    private AnimatorOverrideController animationOverrideController;

    public string DisplayName => displayName;

    public AnimatorOverrideController AnimationOverrideController =>
        animationOverrideController;
}