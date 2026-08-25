using UnityEngine;
[System.Serializable]
public class GrenadeSlot
{
    [SerializeField]
    private GameObject grenadePrefab;

    [SerializeField]
    [Min(0)]
    private int count;

    [SerializeField]
    [Min(0)]
    private int maxCount = 3;

    public GameObject GrenadePrefab => grenadePrefab;

    public int Count => count;

    public int MaxCount => maxCount;

    public bool CanThrow()
    {
        return grenadePrefab != null &&
               count > 0;
    }

    public bool Consume()
    {
        if (count <= 0)
        {
            return false;
        }

        count--;

        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        count = Mathf.Min(
            count + amount,
            maxCount);
    }
}