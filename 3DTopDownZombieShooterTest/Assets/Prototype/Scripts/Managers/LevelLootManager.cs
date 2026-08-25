using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LootEntry
{
    [field: SerializeField] public ItemPickupDef Item { get; private set; }

    [field: SerializeField, Min(0f)]
    public float Weight { get; private set; } = 1f;
}

public class LevelLootManager : MonoBehaviour
{
    private IPooler pooler;

    [SerializeField, Range(0f, 1f)]
    private float dropChance = 0.25f;

    [SerializeField]
    private List<LootEntry> lootTable = new();
    
    public void Init(IPooler pooler)
    {
        this.pooler = pooler;
    }
    public ItemPickupDef GetRandomLoot()
    {
        if (Random.value > dropChance)
            return null;

        if (lootTable.Count == 0)
            return null;

        float totalWeight = 0f;

        foreach (LootEntry entry in lootTable)
        {
            if (entry.Item != null)
                totalWeight += entry.Weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);

        foreach (LootEntry entry in lootTable)
        {
            if (entry.Item == null || entry.Weight <= 0f)
                continue;

            roll -= entry.Weight;

            if (roll <= 0f)
                return entry.Item;
        }

        return null;
    }
    
    public void OnZombieDead(Zombie zombie)
    {
        SpawnLoot(new Vector3(zombie.transform.position.x, 0.2f, zombie.transform.position.z));
    }
    public void SpawnLoot(Vector3 position)
    {
        var loot = GetRandomLoot();
        if (loot != null)
        {
            var go = pooler.Spawn(loot.prefab.gameObject, position, Quaternion.identity);
            var pick = go as Pickable;
            pick.Init(loot);
        }
    }
}