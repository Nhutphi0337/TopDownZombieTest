public class ZombieDeadState : IState
{
    private Zombie zombie;
    public ZombieDeadState(Zombie owner)
    {
        zombie = owner;
    }
    public void Enter()
    {
        zombie.Movement.Stop();

        //zombie.pooler.Spawn();
        AudioManager.Instance.Play(zombie.ZombieDef.DeadSound);

        zombie.Die();
    }
    public void Tick()
    {

    }
    public void Exit()
    {

    }
}
