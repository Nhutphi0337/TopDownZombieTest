using UnityEngine;

public class ZombieAnimationController : MonoBehaviour
{
    [SerializeField]
    private Animator animator;
    private RuntimeAnimatorController baseController;

    private bool isDead;

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private static readonly int DeadHash =
        Animator.StringToHash("Dead");

    private void Awake()
    {
        if (animator == null)
        {
            animator =
                GetComponent<Animator>();
        }

        if (animator != null)
        {
            baseController = animator.runtimeAnimatorController;
        }
    }

    public void SetOverrideAttackAnimator(ZombieAttackDef zombieAttackDef)
    {
        if (animator == null)
        {
            return;
        }

        if (zombieAttackDef == null ||
            zombieAttackDef.AnimationOverrideController == null)
        {
            ResetToBaseAnimator();
            return;
        }

        animator.runtimeAnimatorController =
            zombieAttackDef.AnimationOverrideController;
    }
    public void ResetToBaseAnimator()
    {
        if(baseController != null && animator.runtimeAnimatorController != baseController)
            animator.runtimeAnimatorController = baseController;
    }
    public void PlayAttack()
    {
        if (isDead ||
            animator == null)
        {
            return;
        }

        animator.SetTrigger(
            AttackHash);
    }

    public void SetWalking(bool moving)
    {
        if (isDead ||
            animator == null)
        {
            return;
        }

        animator.SetBool(IsMovingHash, moving);
    }

    public void PlayDeath()
    {
        if (isDead ||
            animator == null)
        {
            return;
        }

        isDead = true;

        animator.SetBool(DeadHash, true);
    }

    public void ResetAnimStates()
    {
        ResetToBaseAnimator();
        animator.SetBool(DeadHash, false);
        animator.SetBool(IsMovingHash, false);
    }
}