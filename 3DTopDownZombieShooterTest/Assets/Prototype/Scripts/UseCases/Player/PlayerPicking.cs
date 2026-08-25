using UnityEngine;
public class PlayerPicking : MonoBehaviour
{
    private Player player;
    public void Init(Player player)
    {
        this.player = player;
    }
    private void OnTriggerEnter(Collider other)
    {
        var pick = false;
        //This is temporary. Will find a better way.
        if (other.TryGetComponent(out Pickable pickable))
        {
            if (pickable.pickableDef.itemType == ItemType.Gun)
            {
                var gunDef = pickable.pickableDef.pickableItem as GunDef;
                if (!player.Equipment.HaveGun(gunDef))
                {
                    player.GunGripController.ClearGun();
                    player.Equipment.PickNewGun(gunDef, player.Equipment.CurrentGunIndex);
                    player.GunController.EquipCurrentGun(player.Equipment.CurrentGunIndex);
                    pick = true;
                }
            }
            if (pickable.pickableDef.itemType == ItemType.Ammo)
            {
                var ammoDef = pickable.pickableDef.pickableItem as AmmoDef;
                pick = player.Equipment.AddAmmo(ammoDef, pickable.pickableDef.amount);
            }
            else if (pickable.pickableDef.itemType == ItemType.HealthPack)
            {
                var hpPack = pickable.pickableDef.pickableItem as HealthPackDef;
                player.Heal(hpPack.healAmount);
                pick = true;
            }
            
            if(pick)
                pickable.Pick();
        }
    }
}
