using UnityEngine;
public class Pickable : MonoBehaviour, IPoolable
{
    //[SerializeField] private Collider triggerCollider;
    private IPooler pooler;
    [field: SerializeField] public ItemPickupDef pickableDef { get; private set; }
    public void Init(ItemPickupDef def)
    {
        pickableDef = def;
    }

    public void Pick()
    {
        AudioManager.Instance.Play(pickableDef.pickSound);
        pickableDef = null;
        pooler.Return(this);
    }

    public void OnReleased()
    {
    }

    public void OnSpawned()
    {
    }

    public void SetPooler(IPooler pooler)
    {
        this.pooler = pooler;
    }
}
