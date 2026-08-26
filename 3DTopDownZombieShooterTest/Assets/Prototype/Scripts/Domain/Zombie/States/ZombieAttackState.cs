public class ZombieAttackState : IState
{
    private readonly Zombie zombie;

    public ZombieAttackState(Zombie zombie)
    {
        this.zombie = zombie;
    }

    public void Enter()
    {
        StartAttack();
    }

    public void Tick()
    {
        if (!zombie.Attack.IsAttacking &&
            !zombie.Attack.IsOnCooldown &&
            zombie.Attack.HasAvailableAttack)
        {
            StartAttack();
        }
    }

    public void Exit()
    {
        zombie.AnimationController.ResetToBaseAnimator();
    }

    private void StartAttack()
    {
        zombie.Attack.StartAttack();

        zombie.AnimationController.SetOverrideAttackAnimator(
            zombie.Attack.CurrentAttackDef);

        zombie.AnimationController.PlayAttack();

        AudioManager.Instance.Play(
            zombie.ZombieDef.AttackSound);
    }
}