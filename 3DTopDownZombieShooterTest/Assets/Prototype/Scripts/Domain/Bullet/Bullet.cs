using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour, IPoolable
{
    private ITeam owner;
    private IPooler pooler;

    private AttackDef attackDef;

    private Rigidbody rb;

    private Vector3 startPosition;
    private float maxTravelDistance;

    private bool isActive;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        startPosition = Vector3.zero;
        maxTravelDistance = 0f;
    }

    public void Init(
    ITeam owner,
    Vector3 position,
    Vector3 direction,
    AttackDef attackDef,
    float speed,
    float range)
    {
        this.owner = owner;
        this.attackDef = attackDef;

        maxTravelDistance = range;

        startPosition = position;

        rb.position = position;
        rb.velocity = direction.normalized * speed;

        isActive = true;
    }

    public void SetPooler(IPooler pooler)
    {
        this.pooler = pooler;
    }

    public void OnSpawned()
    {
        isActive = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void OnReleased()
    {
        isActive = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        startPosition = Vector3.zero;
        maxTravelDistance = 0f;
    }

    private void FixedUpdate()
    {
        if (!isActive)
        {
            return;
        }

        float maxDistanceSqr =
            maxTravelDistance * maxTravelDistance;

        if ((rb.position - startPosition).sqrMagnitude
            >= maxDistanceSqr)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive)
        {
            return;
        }

        if(attackDef == null)
            ReturnToPool();

        if (!other.TryGetComponent<IDamageable>(
            out IDamageable damageable))
        {
            return;
        }

        if (damageable == owner as IDamageable)
        {
            return;
        }

        var attackContext = new AttackContext();
        attackContext.Attacker = owner;
        attackContext.HitCollider = other;
        attackContext.HitPoint = transform.position;

        attackDef.Execute(attackContext);
        
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (!isActive)
        {
            return;
        }

        isActive = false;

        if (pooler == null)
        {
            Debug.LogWarning(
                $"{nameof(Bullet)} has no {nameof(IPooler)}.",
                this);

            gameObject.SetActive(false);
            return;
        }

        pooler.Return(this);
    }
}