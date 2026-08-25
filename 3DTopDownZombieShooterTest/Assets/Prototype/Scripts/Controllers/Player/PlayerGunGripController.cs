using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerGunGripController : MonoBehaviour
{
    [SerializeField]
    private Transform leftHandGrip;
    [SerializeField]
    private Transform rightHandGrip;

    [Header("Hand IK")]
    [SerializeField]
    private TwoBoneIKConstraint rightHandIK;

    [SerializeField]
    private TwoBoneIKConstraint leftHandIK;

    public void SetGun(Gun gun)
    {
        if (gun == null)
        {
            ClearGun();
            return;
        }

        leftHandGrip.SetParent(gun.leftHandGrip);
        leftHandGrip.localPosition = Vector3.zero;
        rightHandGrip.SetParent(gun.rightHandGrip);
        rightHandGrip.localPosition = Vector3.zero;
        
        //SetIKTarget(
        //    rightHandIK,
        //    gun.rightHandGrip);

        //SetIKTarget(
        //    leftHandIK,
        //    gun.leftHandGrip);
    }
    public void DisableFollowing()
    {
        rightHandIK.weight = 0f;
        leftHandIK.weight = 0f;
    }

    public void EnableFollowing()
    {
        leftHandIK.weight = 1f;
        rightHandIK.weight = 1f;
    }
    public void ClearGun()
    {
        leftHandGrip.SetParent(null);
        rightHandGrip.SetParent(null);
        //SetIKTarget(
        //    rightHandIK,
        //    null);

        //SetIKTarget(
        //    leftHandIK,
        //    null);
    }

    //private static void SetIKTarget(
    //    TwoBoneIKConstraint constraint,
    //    Transform target)
    //{
    //    if (constraint == null)
    //    {
    //        return;
    //    }

    //    //constraint.data.target = target;
    //}
}