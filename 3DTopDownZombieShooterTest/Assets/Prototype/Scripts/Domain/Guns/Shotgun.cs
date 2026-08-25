using UnityEngine;

public class Shotgun : Gun
{
    private ShotGunDef shotGunDef;
    public override void Init(GunDef gunDef, ITeam owner)
    {
        base.Init(gunDef, owner);
        shotGunDef = gunDef as ShotGunDef;
    }
    protected override bool FireProjectiles()
    {
        bool fired = false;

        for (int i = 0; i < shotGunDef.PelletCount; i++)
        {
            Vector3 direction = GetSpreadDirection(Muzzle.forward);

            if (SpawnBullet(direction))
                fired = true;
        }

        return fired;
    }

    private Vector3 GetSpreadDirection(Vector3 forward)
    {
        float angle = Random.Range(-shotGunDef.SpreadAngle, shotGunDef.SpreadAngle);
        return Quaternion.AngleAxis(angle, Vector3.up) * forward;
    }
    //private Vector3 GetSpreadDirection(Vector3 forward)
    //{
    //    Vector2 spread = Random.insideUnitCircle * shotGunDef.SpreadAngle;

    //    return Quaternion.AngleAxis(spread.x, Vector3.up) *
    //           Quaternion.AngleAxis(-spread.y, Vector3.right) *
    //           forward;
    //}
}