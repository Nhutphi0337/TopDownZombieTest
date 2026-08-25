using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float movementSpeed = 5f;

    [Header("Camera")]
    [SerializeField]
    private Camera gameplayCamera;

    private Rigidbody playerRigidbody;
    private PlayerInput playerInput;

    private bool movementEnabled = true;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (!movementEnabled)
        {
            return;
        }

        // Input is read by PlayerInput.
        // PlayerMovement consumes the resulting movement intent.
    }

    private void FixedUpdate()
    {
        if (!movementEnabled)
        {
            playerRigidbody.velocity = Vector3.zero;
            return;
        }

        ApplyMovement();
    }

    private void ApplyMovement()
    {
        Vector3 movement =
            CalculateCameraRelativeMovement();

        playerRigidbody.velocity =
            movement * movementSpeed;
    }

    private Vector3 CalculateCameraRelativeMovement()
    {
        if (gameplayCamera == null)
        {
            return Vector3.zero;
        }

        Vector3 cameraForward =
            gameplayCamera.transform.forward;

        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude > Mathf.Epsilon)
        {
            cameraForward.Normalize();
        }

        Vector3 cameraRight =
            gameplayCamera.transform.right;

        cameraRight.y = 0f;

        if (cameraRight.sqrMagnitude > Mathf.Epsilon)
        {
            cameraRight.Normalize();
        }

        Vector3 movement =
            cameraRight * playerInput.MovementInput.x +
            cameraForward * playerInput.MovementInput.y;

        movement.y = 0f;

        return Vector3.ClampMagnitude(
            movement,
            1f);
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        if (!enabled)
        {
            playerRigidbody.velocity =
                Vector3.zero;
        }
    }

    public bool IsMovementEnabled()
    {
        return movementEnabled;
    }
}