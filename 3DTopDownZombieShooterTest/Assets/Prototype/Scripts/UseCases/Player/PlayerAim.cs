using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerAim : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private Camera gameplayCamera;

    [Header("Facing")]
    [SerializeField]
    private float aimDeadzone = 0.1f;

    private Player player;

    private Rigidbody playerRigidbody;
    private Quaternion targetRotation;
    private bool hasAimDirection;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        targetRotation = transform.rotation;
    }

    public void Init(Player player)
    {
        this.player = player;
    }
    private void Update()
    {
        if (player.Movement != null &&
            !player.Movement.IsMovementEnabled())
        {
            return;
        }

        UpdateAimTarget();
    }

    private void FixedUpdate()
    {
        if (!hasAimDirection)
        {
            return;
        }

        playerRigidbody.MoveRotation(targetRotation);
    }

    private void UpdateAimTarget()
    {
        Vector3 aimDirection = CalculateAimDirection();

        if (aimDirection.sqrMagnitude <=
            aimDeadzone * aimDeadzone)
        {
            // Keep the previous rotation.
            return;
        }

        aimDirection.Normalize();

        targetRotation = Quaternion.LookRotation(
            aimDirection,
            Vector3.up);

        hasAimDirection = true;
    }

    private Vector3 CalculateAimDirection()
    {
        if (gameplayCamera == null)
        {
            return Vector3.zero;
        }

#if UNITY_ANDROID
        return CalculateStickAimDirection();
#else
        return CalculateMouseAimDirection();
#endif
    }

    private Vector3 CalculateMouseAimDirection()
    {
        Vector2 mousePosition =
            player.Input.AimInput;

        Ray ray =
            gameplayCamera.ScreenPointToRay(
                mousePosition);

        Plane groundPlane =
            new Plane(
                Vector3.up,
                transform.position);

        if (!groundPlane.Raycast(
            ray,
            out float distance))
        {
            return Vector3.zero;
        }

        Vector3 aimPoint =
            ray.GetPoint(distance);

        Vector3 aimDirection =
            aimPoint - transform.position;

        aimDirection.y = 0f;

        return aimDirection;
    }

    private Vector3 CalculateStickAimDirection()
    {
        Vector2 stickInput =
            Vector2.ClampMagnitude(
                player.Input.AimInput,
                1f);

        if (stickInput.sqrMagnitude <=
            aimDeadzone * aimDeadzone)
        {
            return Vector3.zero;
        }

        Vector3 cameraForward =
            gameplayCamera.transform.forward;

        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude >
            Mathf.Epsilon)
        {
            cameraForward.Normalize();
        }

        Vector3 cameraRight =
            gameplayCamera.transform.right;

        cameraRight.y = 0f;

        if (cameraRight.sqrMagnitude >
            Mathf.Epsilon)
        {
            cameraRight.Normalize();
        }

        Vector3 aimDirection =
            cameraRight * stickInput.x +
            cameraForward * stickInput.y;

        aimDirection.y = 0f;

        return aimDirection;
    }
}