using System;
using System.Collections.Generic;

public class StateMachine
{
    private IState currentState;

    private readonly Dictionary<Type, IState> states = new();

    private readonly Dictionary<Type, List<ITransition>> transitions =
        new();

    public IState CurrentState => currentState;

    public void AddState(IState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        states[state.GetType()] = state;
    }

    public void AddTransition<TFrom, TTo>(
        TTo targetState,
        Func<bool> condition)
        where TFrom : IState
        where TTo : IState
    {
        Type fromType = typeof(TFrom);

        if (!transitions.TryGetValue(
                fromType,
                out List<ITransition> stateTransitions))
        {
            stateTransitions = new List<ITransition>();
            transitions.Add(fromType, stateTransitions);
        }

        stateTransitions.Add(
            new Transition<TFrom, TTo>(
                targetState,
                condition));
    }

    public void SetInitialState<T>()
        where T : IState
    {
        ChangeState<T>();
    }

    public void Tick()
    {
        if (currentState == null)
            return;

        currentState.Tick();

        Type currentType = currentState.GetType();

        if (!transitions.TryGetValue(
                currentType,
                out List<ITransition> stateTransitions))
        {
            return;
        }

        foreach (ITransition transition in stateTransitions)
        {
            if (!transition.ShouldTransition)
                continue;

            ChangeState(transition.TargetState);
            return;
        }
    }

    public void ChangeState<T>()
        where T : IState
    {
        if (!states.TryGetValue(
                typeof(T),
                out IState state))
        {
            throw new InvalidOperationException(
                $"State {typeof(T).Name} has not been registered.");
        }

        ChangeState(state);
    }

    private void ChangeState(IState newState)
    {
        if (newState == null)
            return;

        if (ReferenceEquals(currentState, newState))
            return;

        currentState?.Exit();

        currentState = newState;

        currentState.Enter();
    }
}