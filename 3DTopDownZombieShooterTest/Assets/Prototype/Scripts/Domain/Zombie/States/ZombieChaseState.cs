using UnityEngine;

public class ZombieChaseState : IState
{
    private readonly Zombie zombie;

    public ZombieChaseState(Zombie zombie)
    {
        this.zombie = zombie;
    }

    public void Enter()
    {
        zombie.Movement.SetStop(false);
        zombie.AnimationController.SetWalking(true);
        AudioManager.Instance.Play(zombie.ZombieDef.ChaseSound);
    }

    public void Tick()
    {
        if (zombie.Target == null)
        {
            zombie.Movement.SetStop(true);
            zombie.AnimationController.SetWalking(false);
            return;
        }

        Vector3 flowDirection =
            FlowFieldManager.Instance.GetDirection(
                zombie.transform.position);

        zombie.Movement.SetMoveDirection(flowDirection);
    }

    public void Exit()
    {
        zombie.Movement.SetStop(true);
        zombie.AnimationController.SetWalking(false);
    }
}