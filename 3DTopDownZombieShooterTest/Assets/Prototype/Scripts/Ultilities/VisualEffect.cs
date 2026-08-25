using UnityEngine;

public class VisualEffect : MonoBehaviour, IPoolable
{
    [Header("Lifetime")]
    [SerializeField]
    [Min(0f)]
    private float existenceTime = 1f;

    private IPooler pooler;
    private float timer;

    public void SetPooler(IPooler pooler) => this.pooler = pooler;

    public void OnSpawned()
    {
        timer = existenceTime;
    }

    public void OnReleased()
    {
        timer = 0f;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
            pooler.Return(this);
    }
}