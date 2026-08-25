public readonly struct DamageInfo
{
    public float Amount { get; }
    public object Source { get; }

    public DamageInfo(float amount, object source)
    {
        Amount = amount;
        Source = source;
    }
}