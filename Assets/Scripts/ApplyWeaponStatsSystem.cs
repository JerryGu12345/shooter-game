using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

// This system runs on the server to apply gun stats from PlayerProgression to spawned weapons
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ApplyWeaponStatsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (PlayerProgression.Instance == null) return;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        // Find weapons that need stats applied (damage of 0 means uninitialized)
        foreach ((RefRW<PlayerWeapon> weapon, RefRO<GhostOwner> ghostOwner, Entity weaponEntity)
            in SystemAPI.Query<RefRW<PlayerWeapon>, RefRO<GhostOwner>>().WithEntityAccess())
        {
            // Check if this weapon hasn't been initialized yet
            if (weapon.ValueRO.damage == 0)
            {
                GunData equippedGun = PlayerProgression.Instance.GetEquippedGun();
                
                if (equippedGun != null)
                {
                    weapon.ValueRW.firerate = equippedGun.stats.GetFireRateCooldown();
                    weapon.ValueRW.damage = (int)equippedGun.GetDamage();
                    weapon.ValueRW.bulletSpeed = equippedGun.stats.GetBulletSpeed();
                    weapon.ValueRW.magSize = equippedGun.stats.GetMagSize();
                    
                    Debug.Log($"Applied gun stats: Damage={weapon.ValueRW.damage}, " +
                             $"FireRate={weapon.ValueRW.firerate:F2}s, " +
                             $"BulletSpeed={weapon.ValueRW.bulletSpeed:F2}");
                }
            }
        }

        ecb.Playback(state.EntityManager);
    }
}