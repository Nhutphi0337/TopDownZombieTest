using System;

public class Transition<TFrom, TTo> : ITransition
    where TFrom : IState
    where TTo : IState
{
    private readonly Func<bool> condition;
    private readonly TTo targetState;

    public bool ShouldTransition => condition();

    public IState TargetState => targetState;

    public Transition(
        TTo targetState,
        Func<bool> condition)
    {
        this.targetState = targetState;
        this.condition = condition;
    }
}