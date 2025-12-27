using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct InitializePlayerStatsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        EntityManager entityManager = state.EntityManager;
        
        // Find new players that need initialization (have PlayerStats but not initialized yet)
        foreach ((RefRW<PlayerStats> stats, RefRW<PlayerEquipment> equipment, RefRO<GhostOwner> ghostOwner, Entity entity)
            in SystemAPI.Query<RefRW<PlayerStats>, RefRW<PlayerEquipment>, RefRO<GhostOwner>>().WithEntityAccess())
        {
            // Check if already initialized (health > 0)
            if (stats.ValueRO.maxHealth > 0) continue;
            
            // Initialize from PlayerProgression
            if (PlayerProgression.Instance != null)
            {
                stats.ValueRW.maxHealth = PlayerProgression.Instance.GetMaxHP();
                stats.ValueRW.health = stats.ValueRO.maxHealth;
                stats.ValueRW.medKitHealPerSecond = PlayerProgression.Instance.GetMedKitHealPerSecond();
                
                GunData equippedGun = PlayerProgression.Instance.GetEquippedGun();
                if (equippedGun != null)
                {
                    stats.ValueRW.maxAmmo = equippedGun.stats.GetMagSize();
                    stats.ValueRW.currentAmmo = stats.ValueRO.maxAmmo;
                    
                    equipment.ValueRW.gun1Id = equippedGun.gunId;
                    equipment.ValueRW.gun2Id = -1; // TODO: Add second gun selection
                    equipment.ValueRW.currentSlot = EquipmentSlot.Gun1;
                    equipment.ValueRW.currentItemSize = equippedGun.GetSize();
                }
                
                Debug.Log($"Initialized player stats: HP={stats.ValueRO.maxHealth:F1}, Ammo={stats.ValueRO.maxAmmo}, Size={equipment.ValueRO.currentItemSize:F1}");
            }
        }
        
        ecb.Playback(state.EntityManager);
    }
}