using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private InputActionReference moveAction;

    [Header("Aim")]
    [SerializeField]
    private InputActionReference aimAction;

    [Header("Actions")]
    [SerializeField]
    private InputActionReference shootAction;

    [SerializeField]
    private InputActionReference reloadAction;

    [SerializeField]
    private InputActionReference throwGrenadeAction;

    [SerializeField]
    private InputActionReference switchGunAction;

    [SerializeField]
    private InputActionReference switchGrenadeAction;

    public Vector2 MovementInput
    {
        get
        {
            if (moveAction == null)
                return Vector2.zero;

            return Vector2.ClampMagnitude(
                moveAction.action.ReadValue<Vector2>(),
                1f);
        }
    }

    public Vector2 AimInput
    {
        get
        {
            if (aimAction == null)
                return Vector2.zero;

            return aimAction.action.ReadValue<Vector2>();
        }
    }

    public bool IsMouseAimInput
    {
        get
        {
            if (aimAction == null)
                return false;

            foreach (InputBinding binding in aimAction.action.bindings)
            {
                if (binding.isComposite)
                    continue;

                if (binding.effectivePath.Contains("<Mouse>/position"))
                    return true;
            }

            return false;
        }
    }

    public bool ShootPressedThisFrame
    {
        get
        {
            return shootAction != null &&
                   shootAction.action.WasPressedThisFrame();
        }
    }

    public bool ReloadPressedThisFrame
    {
        get
        {
            return reloadAction != null &&
                   reloadAction.action.WasPressedThisFrame();
        }
    }

    public bool ThrowGrenadePressedThisFrame
    {
        get
        {
            return throwGrenadeAction != null &&
                   throwGrenadeAction.action.WasPressedThisFrame();
        }
    }

    public bool ThrowGrenadeReleasedThisFrame
    {
        get
        {
            return throwGrenadeAction != null &&
                   throwGrenadeAction.action.WasReleasedThisFrame();
        }
    }

    public bool SwitchGunPressedThisFrame
    {
        get
        {
            return switchGunAction != null &&
                   switchGunAction.action.WasPressedThisFrame();
        }
    }

    public bool SwitchGrenadePressedThisFrame
    {
        get
        {
            return switchGrenadeAction != null &&
                   switchGrenadeAction.action.WasPressedThisFrame();
        }
    }

    public void DisableActions()
    {
        DisableAction(moveAction);
        DisableAction(aimAction);
        DisableAction(shootAction);
        DisableAction(reloadAction);
        DisableAction(throwGrenadeAction);
        DisableAction(switchGunAction);
        DisableAction(switchGrenadeAction);
    }

    public void EnableActions()
    {
        EnableAction(moveAction);
        EnableAction(aimAction);
        EnableAction(shootAction);
        EnableAction(reloadAction);
        EnableAction(throwGrenadeAction);
        EnableAction(switchGunAction);
        EnableAction(switchGrenadeAction);
    }

    private void OnEnable()
    {
        EnableActions();
    }

    private void OnDisable()
    {
        DisableActions();
    }

    private static void EnableAction(
        InputActionReference actionReference)
    {
        if (actionReference != null)
            actionReference.action.Enable();
    }

    private static void DisableAction(
        InputActionReference actionReference)
    {
        if (actionReference != null)
            actionReference.action.Disable();
    }
}