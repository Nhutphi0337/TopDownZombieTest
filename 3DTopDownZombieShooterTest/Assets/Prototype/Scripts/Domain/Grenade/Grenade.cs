using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grenade : MonoBehaviour, IPoolable
{
    private ITeam owner;

    private GrenadeDef grenadeDef;
    private IPooler pooler;
    private Rigidbody rb;

    private Vector3 startPosition;
    private Vector3 throwDirection;

    private float maxThrowDistance;
    private float remainingFuseTime;

    private bool isActive;
    private bool hasBeenThrown;
    private bool distanceLimitReached;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetPooler(IPooler pooler)
    {
        this.pooler = pooler;
    }

    public void Initialize(
        ITeam owner,
        GrenadeDef grenadeDef,
        Vector3 direction,
        float throwForce,
        float maxDistance,
        float fuseTime)
    {
        this.owner = owner;
        this.grenadeDef = grenadeDef;

        throwDirection =
            Vector3.ProjectOnPlane(
                direction,
                Vector3.up).normalized;

        maxThrowDistance =
            Mathf.Max(
                0f,
                maxDistance);

        remainingFuseTime =
            Mathf.Max(
                0f,
                fuseTime);

        startPosition =
            transform.position;

        hasBeenThrown = true;
        distanceLimitReached = false;

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        rb.AddForce(
            throwDirection * throwForce,
            ForceMode.Impulse);
    }

    public void OnSpawned()
    {
        rb.isKinematic = false;
        rb.useGravity = true;

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        startPosition =
            transform.position;

        throwDirection = Vector3.zero;

        maxThrowDistance = 0f;
        remainingFuseTime = 0f;

        isActive = true;
        hasBeenThrown = false;
        distanceLimitReached = false;
    }

    public void OnReleased()
    {
        rb.isKinematic = false;
        rb.useGravity = true;

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        throwDirection = Vector3.zero;

        maxThrowDistance = 0f;
        remainingFuseTime = 0f;

        isActive = false;
        hasBeenThrown = false;
        distanceLimitReached = false;
    }

    private void Update()
    {
        if (!isActive ||
            !hasBeenThrown)
        {
            return;
        }

        UpdateFuse();

        if (!distanceLimitReached)
        {
            CheckThrowDistance();
        }
    }

    private void UpdateFuse()
    {
        remainingFuseTime -=
            Time.deltaTime;

        if (remainingFuseTime <= 0f)
        {
            Explode();
        }
    }

    private void CheckThrowDistance()
    {
        Vector3 displacement =
            transform.position -
            startPosition;

        Vector3 horizontalDisplacement =
            Vector3.ProjectOnPlane(
                displacement,
                Vector3.up);

        if (horizontalDisplacement.sqrMagnitude <
            maxThrowDistance * maxThrowDistance)
        {
            return;
        }

        distanceLimitReached = true;

        /*
         * We have reached the gameplay distance
         * limit.
         *
         * Do NOT disable the grenade.
         * Do NOT return it to the pool.
         *
         * Simply remove the horizontal movement
         * so gravity can make it fall naturally.
         */
        Vector3 velocity =
            rb.velocity;

        velocity.x = 0f;
        velocity.z = 0f;

        rb.velocity = velocity;
    }

    private void Explode()
    {
        if (!isActive)
        {
            return;
        }

        isActive = false;

        if (grenadeDef != null && grenadeDef.AttackDef != null)
        {
            pooler.Spawn(grenadeDef.ExplosionVfx
                , new Vector3(transform.position.x, 0.1f, transform.position.z), 
                Quaternion.identity);
            AudioManager.Instance.Play(grenadeDef.ExplosionSound);

            var attCtx = new AttackContext();
            attCtx.Attacker = owner;
            attCtx.HitPoint = new Vector3(transform.position.x, 0.1f, transform.position.z);
            grenadeDef.AttackDef.Execute(attCtx);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (pooler == null)
        {
            Debug.LogWarning(
                $"{nameof(Grenade)} has no {nameof(IPooler)}.",
                this);

            gameObject.SetActive(false);
            return;
        }

        pooler.Return(this);
    }
}