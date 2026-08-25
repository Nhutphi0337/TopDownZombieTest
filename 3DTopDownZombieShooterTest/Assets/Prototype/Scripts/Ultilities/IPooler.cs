using UnityEngine;

public interface IPooler
{
    IPoolable Spawn(GameObject prefab);

    IPoolable Spawn(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation);

    void Return(IPoolable instance);
}