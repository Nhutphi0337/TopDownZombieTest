public interface ITransition
{
    bool ShouldTransition { get; }
    IState TargetState { get; }
}