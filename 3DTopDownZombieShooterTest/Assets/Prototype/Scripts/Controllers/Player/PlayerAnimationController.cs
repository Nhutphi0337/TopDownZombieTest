using UnityEngine;
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    private Rigidbody playerRigidbody;

    private RuntimeAnimatorController baseController;

    private bool isDead;

    private static readonly int MoveXHash =
        Animator.StringToHash("MoveX");

    private static readonly int MoveYHash =
        Animator.StringToHash("MoveY");

    private static readonly int MoveSpeedHash =
        Animator.StringToHash("MoveSpeed");

    private static readonly int ShootHash =
        Animator.StringToHash("Shoot");

    private static readonly int ThrowGrenadeHash =
        Animator.StringToHash("ThrowGrenade");

    private static readonly int DeadHash =
        Animator.StringToHash("Dead");

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            baseController = animator.runtimeAnimatorController;
        }
    }

    private void FixedUpdate()
    {
        UpdateLocomotion();
    }

    private void UpdateLocomotion()
    {
        if (animator == null ||
            playerRigidbody == null)
        {
            return;
        }

        Vector3 velocity =
            playerRigidbody.velocity;

        velocity.y = 0f;

        float speed =
            velocity.magnitude;

        if (speed <= 0.01f)
        {
            animator.SetFloat(
                MoveXHash,
                0f);

            animator.SetFloat(
                MoveYHash,
                0f);

            animator.SetFloat(
                MoveSpeedHash,
                0f);

            return;
        }

        Vector3 movementDirection =
            velocity / speed;

        movementDirection.y = 0f;

        Vector3 facingDirection =
            transform.forward;

        facingDirection.y = 0f;

        if (facingDirection.sqrMagnitude <=
            Mathf.Epsilon)
        {
            facingDirection =
                Vector3.forward;
        }
        else
        {
            facingDirection.Normalize();
        }

        Quaternion facingRotation =
            Quaternion.LookRotation(
                facingDirection,
                Vector3.up);

        Vector3 localMovementDirection =
            Quaternion.Inverse(facingRotation) *
            movementDirection;

        localMovementDirection.y = 0f;

        if (localMovementDirection.sqrMagnitude > 1f)
        {
            localMovementDirection.Normalize();
        }

        animator.SetFloat(
            MoveXHash,
            localMovementDirection.x);

        animator.SetFloat(
            MoveYHash,
            localMovementDirection.z);

        animator.SetFloat(
            MoveSpeedHash,
            speed);
    }

    /// <summary>
    /// Applies the animation variant associated
    /// with the equipped gun type.
    /// </summary>
    public void SetGunType(GunType gunType)
    {
        if (animator == null)
        {
            return;
        }

        if (gunType == null ||
            gunType.AnimationOverrideController == null)
        {
            animator.runtimeAnimatorController =
                baseController;

            return;
        }

        animator.runtimeAnimatorController =
            gunType.AnimationOverrideController;
    }

    /// <summary>
    /// Requests the player's shooting animation.
    /// The gun system decides whether the shot is allowed.
    /// </summary>
    public void PlayShoot()
    {
        if (isDead ||
            animator == null)
        {
            return;
        }

        animator.SetTrigger(
            ShootHash);
    }

    /// <summary>
    /// Requests the player's grenade throw animation.
    /// </summary>
    public void PlayThrowGrenade()
    {
        if (isDead ||
            animator == null)
        {
            return;
        }

        animator.SetTrigger(
            ThrowGrenadeHash);
    }

    /// <summary>
    /// Puts the player into the terminal death animation state.
    /// </summary>
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

    public bool IsDead()
    {
        return isDead;
    }
}