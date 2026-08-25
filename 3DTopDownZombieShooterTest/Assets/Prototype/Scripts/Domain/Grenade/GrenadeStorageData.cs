using UnityEngine;

[System.Serializable]
public class GrenadeStorageData
{
    [SerializeField]
    private GrenadeDef grenadeDef;

    [SerializeField]
    [Min(0)]
    private int amount;

    [SerializeField]
    [Min(0)]
    private int maxAmount;

    public GrenadeDef GrenadeDef => grenadeDef;

    public int Amount => amount;

    public int MaxAmount => maxAmount;

    public void SetGrenadeDef(GrenadeDef grenadeDef)
    {
        this.grenadeDef = grenadeDef;
        maxAmount = grenadeDef.MaxAmount;
    }

    public bool CanConsume(int requestedAmount)
    {
        return requestedAmount > 0 &&
               amount >= requestedAmount;
    }

    public bool Consume(int requestedAmount)
    {
        if (!CanConsume(requestedAmount))
        {
            return false;
        }

        amount -= requestedAmount;

        return true;
    }

    public void Add(int amountToAdd)
    {
        if (amountToAdd <= 0)
        {
            return;
        }

        amount = Mathf.Min(
            amount + amountToAdd,
            maxAmount);
    }

    public void ResetAmount()
    {
        amount = 0;
    }
}