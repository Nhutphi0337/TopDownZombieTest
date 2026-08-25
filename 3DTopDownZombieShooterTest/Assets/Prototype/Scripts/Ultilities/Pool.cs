using System.Collections.Generic;
using UnityEngine;

public class Pool
{
    private readonly GameObject prefab;
    private readonly Transform parent;

    private readonly Queue<IPoolable> availableInstances =
        new Queue<IPoolable>();

    private readonly HashSet<IPoolable> instances =
        new HashSet<IPoolable>();

    public Pool(GameObject prefab, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    public IPoolable Get(IPooler pooler)
    {
        IPoolable instance;

        if (availableInstances.Count > 0)
        {
            instance = availableInstances.Dequeue();
        }
        else
        {
            instance = CreateInstance();

            if (instance == null)
            {
                return null;
            }
        }

        Component component = instance as Component;

        if (component == null)
        {
            Debug.LogError(
                $"Pooled prefab '{prefab.name}' must contain a Component implementing IPoolable.");

            return null;
        }

        instance.SetPooler(pooler);

        component.transform.SetParent(null);
        component.gameObject.SetActive(true);
        instance.OnSpawned();

        return instance;
    }

    public IPoolable Get(
        IPooler pooler,
        Vector3 position,
        Quaternion rotation)
    {
        IPoolable instance;

        if (availableInstances.Count > 0)
        {
            instance = availableInstances.Dequeue();
        }
        else
        {
            instance = CreateInstance();

            if (instance == null)
            {
                return null;
            }
        }

        Component component = instance as Component;

        if (component == null)
        {
            Debug.LogError(
                $"Pooled prefab '{prefab.name}' must contain a Component implementing IPoolable.");

            return null;
        }

        instance.SetPooler(pooler);
        // The object is inactive at this point.
        // Position it before activating it so physics cannot
        // process it at its previous position.
        component.transform.SetParent(null);
        component.transform.SetPositionAndRotation(
            position,
            rotation);

        component.gameObject.SetActive(true);

        instance.OnSpawned();

        return instance;
    }

    public void Return(IPoolable instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!instances.Contains(instance))
        {
            Debug.LogWarning(
                $"Attempted to return an instance that does not belong to the pool for '{prefab.name}'.");

            return;
        }

        Component component = instance as Component;

        if (component == null)
        {
            return;
        }

        instance.OnReleased();

        component.gameObject.SetActive(false);
        component.transform.SetParent(parent);

        availableInstances.Enqueue(instance);
    }

    private IPoolable CreateInstance()
    {
        GameObject instanceObject = Object.Instantiate(
            prefab,
            parent);

        instanceObject.SetActive(false);

        IPoolable instance =
            instanceObject.GetComponent<IPoolable>();

        if (instance == null)
        {
            Object.Destroy(instanceObject);

            Debug.LogError(
                $"Prefab '{prefab.name}' must contain a component implementing IPoolable.");

            return null;
        }

        instances.Add(instance);

        return instance;
    }
}