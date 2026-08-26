using UnityEngine;

public class ZombieMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float directionSmoothness = 10f;
    [SerializeField] private float rotationSpeed = 720f;

    private float moveSpeed;

    [Header("Crowd")]
    [SerializeField] private LayerMask zombieMask;
    [SerializeField] private float crowdRadius = 1.2f;
    [SerializeField] private float crowdStrength = 1.5f;
    [SerializeField] private int maxNearbyZombies = 16;
    [SerializeField] private float separationUpdateInterval = 0.1f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float avoidanceDistance = 0.75f;
    [SerializeField] private float avoidanceHeight = 0.5f;
    [SerializeField] private float wallClearance = 0.1f;

    [Header("Wall Following")]
    [SerializeField] private float wallFollowDuration = 1.5f;
    [SerializeField] private float wallFollowLookAhead = 0.5f;

    [Header("Flow Field Recovery")]
    [SerializeField] private float recoveryDistance = 0.15f;
    [SerializeField] private float recoveryStrength = 2f;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    private Vector3 desiredDirection;
    private Vector3 currentDirection;

    private Collider[] nearbyZombies;

    private Vector3 cachedSeparation;
    private float separationTimer;

    private bool isWallFollowing;
    private Vector3 wallNormal;
    private Vector3 wallDirection;
    private float wallFollowTimer;

    private bool isRecoveringFromInvalidCell;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        nearbyZombies = new Collider[maxNearbyZombies];

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void SetMoveDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        desiredDirection = direction;
    }

    public void Stop()
    {
        desiredDirection = Vector3.zero;
        currentDirection = Vector3.zero;
        cachedSeparation = Vector3.zero;
        separationTimer = 0f;
        isWallFollowing = false;
        wallFollowTimer = 0f;
        isRecoveringFromInvalidCell = false;
    }

    private void FixedUpdate()
    {
        UpdateMovementDirection();
        Move();
        Rotate();
    }

    private void UpdateMovementDirection()
    {
        FlowFieldManager flowField = FlowFieldManager.Instance;

        if (flowField == null)
        {
            currentDirection = Vector3.Slerp(currentDirection, desiredDirection, directionSmoothness * Time.fixedDeltaTime);
            return;
        }

        bool walkable = flowField.IsWalkable(transform.position);

        if (!walkable)
        {
            isRecoveringFromInvalidCell = true;
            isWallFollowing = false;
            wallFollowTimer = 0f;

            if (flowField.TryGetRecoveryDirection(transform.position, out Vector3 recoveryDirection))
            {
                Vector3 safeRecoveryDirection = GetRecoveryDirection(recoveryDirection);

                currentDirection = Vector3.Slerp(
                    currentDirection,
                    safeRecoveryDirection,
                    directionSmoothness * Time.fixedDeltaTime);

                currentDirection.y = 0f;

                if (currentDirection.sqrMagnitude > 1f)
                    currentDirection.Normalize();

                return;
            }

            currentDirection = Vector3.Slerp(
                currentDirection,
                Vector3.zero,
                directionSmoothness * Time.fixedDeltaTime);

            return;
        }

        if (isRecoveringFromInvalidCell)
        {
            isRecoveringFromInvalidCell = false;
            currentDirection = Vector3.zero;
        }

        Vector3 flowDirection = GetFlowFieldDirection();

        if (flowDirection.sqrMagnitude < 0.0001f)
        {
            currentDirection = Vector3.Slerp(
                currentDirection,
                Vector3.zero,
                directionSmoothness * Time.fixedDeltaTime);

            return;
        }

        Vector3 direction = GetCrowdControlledDirection(flowDirection);
        direction = GetObstacleAvoidanceDirection(direction);

        if (direction.sqrMagnitude < 0.0001f)
        {
            currentDirection = Vector3.Slerp(
                currentDirection,
                Vector3.zero,
                directionSmoothness * Time.fixedDeltaTime);

            return;
        }

        currentDirection = Vector3.Slerp(
            currentDirection,
            direction,
            directionSmoothness * Time.fixedDeltaTime);

        currentDirection.y = 0f;

        if (currentDirection.sqrMagnitude > 1f)
            currentDirection.Normalize();
    }
    private Vector3 GetFlowFieldDirection()
    {
        Vector3 direction = FlowFieldManager.Instance.GetDirection(transform.position);

        direction.y = 0f;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        return direction;
    }

    private Vector3 GetRecoveryDirection(Vector3 recoveryDirection)
    {
        recoveryDirection.y = 0f;

        if (recoveryDirection.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        recoveryDirection.Normalize();

        Vector3 origin = GetCastOrigin();
        float radius = GetWorldRadius();

        Vector3 safeDirection = GetObstacleAvoidanceDirection(recoveryDirection);

        if (safeDirection.sqrMagnitude < 0.0001f)
            return recoveryDirection;

        Vector3 separation = CalculateSeparation();

        if (separation.sqrMagnitude > 0.0001f)
        {
            Vector3 adjusted = safeDirection + separation * (crowdStrength * 0.25f);
            adjusted.y = 0f;

            if (adjusted.sqrMagnitude > 0.0001f)
                safeDirection = adjusted.normalized;
        }

        return safeDirection;
    }

    private Vector3 GetCrowdControlledDirection(Vector3 baseDirection)
    {
        Vector3 direction = baseDirection;

        if (direction.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        Vector3 separation = CalculateSeparation();

        if (separation.sqrMagnitude < 0.0001f)
            return direction;

        Vector3 adjusted = direction + separation * crowdStrength;
        adjusted.y = 0f;

        if (adjusted.sqrMagnitude < 0.0001f)
            return direction;

        adjusted.Normalize();

        return adjusted;
    }

    private Vector3 CalculateSeparation()
    {
        separationTimer -= Time.fixedDeltaTime;

        if (separationTimer > 0f)
            return cachedSeparation;

        separationTimer = separationUpdateInterval;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            crowdRadius,
            nearbyZombies,
            zombieMask,
            QueryTriggerInteraction.Ignore);

        Vector3 separation = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            Collider other = nearbyZombies[i];

            if (other == null || other == capsuleCollider)
                continue;

            Vector3 offset = transform.position - other.transform.position;
            offset.y = 0f;

            float distanceSqr = offset.sqrMagnitude;

            if (distanceSqr < 0.0001f)
                continue;

            float distance = Mathf.Sqrt(distanceSqr);
            float weight = 1f - Mathf.Clamp01(distance / crowdRadius);

            separation += offset / distance * weight;
        }

        cachedSeparation = separation.sqrMagnitude < 0.0001f ? Vector3.zero : separation.normalized;

        return cachedSeparation;
    }

    private Vector3 GetObstacleAvoidanceDirection(Vector3 desired)
    {
        float radius = GetWorldRadius();
        Vector3 origin = GetCastOrigin();

        if (isWallFollowing)
        {
            wallFollowTimer -= Time.fixedDeltaTime;

            if (CanMoveInDirection(origin, desired, radius))
            {
                isWallFollowing = false;
                return desired;
            }

            if (wallFollowTimer <= 0f)
            {
                isWallFollowing = false;

                Vector3 escape = FindEscapeDirection(desired, origin, radius);

                if (escape.sqrMagnitude > 0.0001f)
                    return escape;

                return desired;
            }

            Vector3 followDirection = GetWallFollowDirection(desired, origin, radius);

            if (followDirection.sqrMagnitude > 0.0001f)
                return followDirection;

            isWallFollowing = false;
        }

        if (CanMoveInDirection(origin, desired, radius))
            return desired;

        if (TryStartWallFollowing(desired, origin, radius))
            return wallDirection;

        Vector3 escapeDirection = FindEscapeDirection(desired, origin, radius);

        if (escapeDirection.sqrMagnitude > 0.0001f)
            return escapeDirection;

        return Vector3.zero;
    }

    private bool TryStartWallFollowing(Vector3 desired, Vector3 origin, float radius)
    {
        if (!Physics.SphereCast(
                origin,
                radius,
                desired,
                out RaycastHit hit,
                avoidanceDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        wallNormal = hit.normal;
        wallNormal.y = 0f;

        if (wallNormal.sqrMagnitude < 0.0001f)
            return false;

        wallNormal.Normalize();

        Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal);
        tangent.y = 0f;

        if (tangent.sqrMagnitude < 0.0001f)
            return false;

        tangent.Normalize();

        Vector3 opposite = -tangent;

        float tangentScore = GetWallDirectionScore(tangent, desired, origin, radius);
        float oppositeScore = GetWallDirectionScore(opposite, desired, origin, radius);

        if (tangentScore <= 0f && oppositeScore <= 0f)
            return false;

        wallDirection = tangentScore >= oppositeScore ? tangent : opposite;
        isWallFollowing = true;
        wallFollowTimer = wallFollowDuration;

        return true;
    }

    private Vector3 GetWallFollowDirection(Vector3 desired, Vector3 origin, float radius)
    {
        Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal);
        tangent.y = 0f;

        if (tangent.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        tangent.Normalize();

        Vector3 opposite = -tangent;

        if (CanMoveInDirection(origin, wallDirection, radius))
            return wallDirection;

        float tangentScore = GetWallDirectionScore(tangent, desired, origin, radius);
        float oppositeScore = GetWallDirectionScore(opposite, desired, origin, radius);

        if (tangentScore <= 0f && oppositeScore <= 0f)
            return Vector3.zero;

        wallDirection = tangentScore >= oppositeScore ? tangent : opposite;

        return wallDirection;
    }

    private float GetWallDirectionScore(Vector3 direction, Vector3 desired, Vector3 origin, float radius)
    {
        if (!CanMoveInDirection(origin, direction, radius))
            return -1f;

        float alignment = Vector3.Dot(direction, desired);
        Vector3 lookAhead = origin + direction * wallFollowLookAhead;

        if (!CanMoveInDirection(lookAhead, direction, radius))
            return -1f;

        return alignment;
    }

    private Vector3 FindEscapeDirection(Vector3 desired, Vector3 origin, float radius)
    {
        Vector3 bestDirection = Vector3.zero;
        float bestScore = float.NegativeInfinity;

        float[] angles = { 45f, -45f, 90f, -90f, 135f, -135f, 180f };

        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 candidate = Quaternion.Euler(0f, angles[i], 0f) * desired;
            candidate.y = 0f;

            if (candidate.sqrMagnitude < 0.0001f)
                continue;

            candidate.Normalize();

            if (!CanMoveInDirection(origin, candidate, radius))
                continue;

            float score = Vector3.Dot(desired, candidate);

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = candidate;
            }
        }

        return bestDirection;
    }

    private bool CanMoveInDirection(Vector3 origin, Vector3 direction, float radius)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();

        float castRadius = Mathf.Max(radius - wallClearance, 0.01f);

        return !Physics.SphereCast(
            origin,
            castRadius,
            direction,
            out _,
            avoidanceDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    private void Move()
    {
        Vector3 direction = currentDirection;

        if (direction.sqrMagnitude < 0.0001f)
        {
            StopHorizontalMovement();
            return;
        }

        Vector3 velocity = direction * moveSpeed;
        velocity.y = rb.velocity.y;
        rb.velocity = velocity;
    }

    private void Rotate()
    {
        Vector3 direction = currentDirection;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        Quaternion newRotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

        rb.MoveRotation(newRotation);
    }

    private Vector3 GetCastOrigin()
    {
        Vector3 center = transform.TransformPoint(capsuleCollider.center);

        return new Vector3(center.x, transform.position.y + avoidanceHeight, center.z);
    }

    private float GetWorldRadius()
    {
        Vector3 scale = transform.lossyScale;
        float horizontalScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));

        return capsuleCollider.radius * horizontalScale;
    }

    private void StopHorizontalMovement()
    {
        Vector3 velocity = rb.velocity;
        velocity.x = 0f;
        velocity.z = 0f;
        rb.velocity = velocity;
    }

    private void OnDrawGizmosSelected()
    {
        if (!isRecoveringFromInvalidCell)
            return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, GetWorldRadius() + recoveryDistance);
    }
}