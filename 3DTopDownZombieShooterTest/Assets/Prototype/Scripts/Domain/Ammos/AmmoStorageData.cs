using UnityEngine;

[System.Serializable]
public class AmmoStorageData
{
    [field: SerializeField]
    public AmmoDef AmmoDef { get; private set; }

    [SerializeField]
    [Min(0)]
    private int amount;
    public int Amount => amount;

    public void SetAmmoDef(AmmoDef ammoDef)
    {
        AmmoDef = ammoDef;
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

        amount += amountToAdd;
    }

    public void ResetAmount() { amount = 0; }
}