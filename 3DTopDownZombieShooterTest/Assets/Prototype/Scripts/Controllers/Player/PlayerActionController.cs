using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerGunController))]
[RequireComponent(typeof(PlayerGrenadeController))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerActionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerInput playerInput;

    [SerializeField]
    private PlayerMovement playerMovement;

    [SerializeField]
    private PlayerGunController gunController;

    [SerializeField]
    private PlayerGrenadeController grenadeController;

    private bool isDead;

    private void Awake()
    {
        if (playerInput == null)
        {
            playerInput =
                GetComponent<PlayerInput>();
        }

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        if (gunController == null)
        {
            gunController =
                GetComponent<PlayerGunController>();
        }

        if (grenadeController == null)
        {
            grenadeController =
                GetComponent<PlayerGrenadeController>();
        }
        
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        if (playerInput.ShootPressedThisFrame)
        {
            RequestShoot();
        }

        if (playerInput.ThrowGrenadePressedThisFrame)
        {
            RequestThrowGrenade();
        }

        if (playerInput.ThrowGrenadeReleasedThisFrame)
        {
            RequestThrowGrenadeRelease();
        }

        if (playerInput.SwitchGunPressedThisFrame)
        {
            RequestSwitchGun();
        }

        if (playerInput.SwitchGrenadePressedThisFrame)
        {
            RequestSwitchGrenade();
        }
    }

    public bool RequestShoot()
    {
        if (isDead)
        {
            return false;
        }

        return gunController != null &&
               gunController.TryShoot();
    }

    public bool RequestThrowGrenade()
    {
        if (isDead)
        {
            return false;
        }

        if (grenadeController == null)
        {
            return false;
        }

        return grenadeController.StartThrow(
            transform.forward);
    }

    private bool RequestThrowGrenadeRelease()
    {
        if (isDead)
        {
            return false;
        }

        if (grenadeController == null)
        {
            return false;
        }

        bool thrown =
            grenadeController.ReleaseThrow();

        if (!thrown)
        {
            return false;
        }
        return true;
    }

    public bool RequestSwitchGun()
    {
        if (isDead)
        {
            return false;
        }

        return gunController != null &&
               gunController.SwitchGun();
    }

    public bool RequestSwitchGrenade()
    {
        if (isDead)
        {
            return false;
        }

        return grenadeController != null &&
               grenadeController.SwitchGrenade();
    }

    public bool RequestDeath()
    {
        if (isDead)
        {
            return false;
        }

        isDead = true;

        if (grenadeController != null)
        {
            grenadeController.CancelThrow();
        }

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(false);
        }

        //if (animationController != null)
        //{
        //    animationController.PlayDeath();
        //}

        return true;
    }

    public bool IsDead()
    {
        return isDead;
    }
}