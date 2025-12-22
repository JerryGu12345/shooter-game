using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

// This system runs on the server to apply weapon stats from PlayerProgression to spawned weapons
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ApplyWeaponStatsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (PlayerProgression.Instance == null) return;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        // Find weapons that need stats applied (they have GhostOwner but default firerate)
        foreach ((RefRW<PlayerWeapon> weapon, RefRO<GhostOwner> ghostOwner, Entity weaponEntity)
            in SystemAPI.Query<RefRW<PlayerWeapon>, RefRO<GhostOwner>>().WithEntityAccess())
        {
            // Check if this weapon has default stats (firerate of 2.0 means it hasn't been customized yet)
            if (weapon.ValueRO.firerate == 2.0f && weapon.ValueRO.damage == 0)
            {
                // Get the equipped weapon for this network ID
                // Note: We need a way to pass the weapon ID from client to server
                // For now, we'll use the equipped weapon from PlayerProgression
                WeaponData equippedWeapon = PlayerProgression.Instance.GetEquippedWeapon();
                
                if (equippedWeapon != null)
                {
                    weapon.ValueRW.firerate = equippedWeapon.GetCurrentFireRate();
                    weapon.ValueRW.damage = equippedWeapon.GetCurrentDamage();
                    
                    Debug.Log($"Applied weapon stats: Damage={weapon.ValueRW.damage}, FireRate={weapon.ValueRW.firerate}");
                }
            }
        }

        ecb.Playback(state.EntityManager);
    }
}