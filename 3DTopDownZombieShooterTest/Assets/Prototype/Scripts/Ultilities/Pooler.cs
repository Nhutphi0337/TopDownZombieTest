using System.Collections.Generic;
using UnityEngine;

public class Pooler : MonoBehaviour, IPooler
{
    private readonly Dictionary<GameObject, Pool> pools =
        new Dictionary<GameObject, Pool>();

    private readonly Dictionary<IPoolable, Pool> instancePools =
        new Dictionary<IPoolable, Pool>();

    public IPoolable Spawn(GameObject prefab)
    {
        return Spawn(
            prefab,
            prefab.transform.position,
            prefab.transform.rotation);
    }

    public IPoolable Spawn(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError(
                $"{nameof(Pooler)} cannot spawn a null prefab.",
                this);

            return null;
        }

        Pool pool = GetOrCreatePool(prefab);

        IPoolable instance = pool.Get(
            this,
            position,
            rotation);

        if (instance == null)
        {
            return null;
        }

        instancePools[instance] = pool;

        return instance;
    }

    public void Return(IPoolable instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!instancePools.TryGetValue(
                instance,
                out Pool pool))
        {
            Debug.LogWarning(
                $"{nameof(Pooler)} received an object that was not spawned by this Pooler.",
                this);

            return;
        }

        pool.Return(instance);
    }

    private Pool GetOrCreatePool(GameObject prefab)
    {
        if (pools.TryGetValue(
                prefab,
                out Pool existingPool))
        {
            return existingPool;
        }

        Pool newPool = new Pool(
            prefab,
            transform);

        pools.Add(prefab, newPool);

        return newPool;
    }
}