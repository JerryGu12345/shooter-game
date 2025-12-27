using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct ReloadSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<NetcodePlayerInput> input, RefRW<PlayerStats> stats, RefRO<PlayerEquipment> equipment)
            in SystemAPI.Query<RefRO<NetcodePlayerInput>, RefRW<PlayerStats>, RefRO<PlayerEquipment>>().WithAll<Simulate>())
        {
            // Handle reloading
            if (stats.ValueRO.isReloading)
            {
                stats.ValueRW.reloadTimer -= SystemAPI.Time.DeltaTime;
                if (stats.ValueRW.reloadTimer <= 0)
                {
                    // Complete reload
                    stats.ValueRW.currentAmmo = stats.ValueRO.maxAmmo;
                    stats.ValueRW.isReloading = false;
                    Debug.Log("Reload complete");
                }
                continue;
            }
            
            // Start reload if requested or auto-reload
            bool reloadRequested = input.ValueRO.reload.IsSet;
            bool autoReload = input.ValueRO.shoot.IsSet && stats.ValueRO.currentAmmo <= 0;
            
            if ((reloadRequested || autoReload) && 
                stats.ValueRO.currentAmmo < stats.ValueRO.maxAmmo &&
                !stats.ValueRO.isReloading &&
                (equipment.ValueRO.currentSlot == EquipmentSlot.Gun1 || equipment.ValueRO.currentSlot == EquipmentSlot.Gun2))
            {
                float gunSize = equipment.ValueRO.currentItemSize;
                float reloadTime = 0.5f * gunSize;
                
                stats.ValueRW.isReloading = true;
                stats.ValueRW.reloadTimer = reloadTime;
                
                Debug.Log($"Reloading... {reloadTime:F2}s");
            }
        }
    }
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct MedKitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<NetcodePlayerInput> input, RefRW<PlayerStats> stats, RefRO<PlayerEquipment> equipment)
            in SystemAPI.Query<RefRO<NetcodePlayerInput>, RefRW<PlayerStats>, RefRO<PlayerEquipment>>().WithAll<Simulate>())
        {
            // Only work with med kit if equipped
            if (equipment.ValueRO.currentSlot != EquipmentSlot.MedKit)
            {
                stats.ValueRW.isUsingMedKit = false;
                stats.ValueRW.medKitUseTimer = 0;
                stats.ValueRW.medKitAccumulatedHeal = 0;
                continue;
            }
            
            // Using med kit (hold mouse button)
            if (input.ValueRO.useMedKit && stats.ValueRO.health < stats.ValueRO.maxHealth)
            {
                if (!stats.ValueRO.isUsingMedKit)
                {
                    stats.ValueRW.isUsingMedKit = true;
                    stats.ValueRW.medKitUseTimer = 0;
                    stats.ValueRW.medKitAccumulatedHeal = 0;
                    Debug.Log("Started using med kit");
                }
                
                stats.ValueRW.medKitUseTimer += SystemAPI.Time.DeltaTime;
                stats.ValueRW.medKitAccumulatedHeal += stats.ValueRO.medKitHealPerSecond * SystemAPI.Time.DeltaTime;
                
                // Apply healing every 1 second of accumulated healing
                while (stats.ValueRW.medKitAccumulatedHeal >= 1.0f)
                {
                    stats.ValueRW.health = math.min(stats.ValueRW.health + 1.0f, stats.ValueRO.maxHealth);
                    stats.ValueRW.medKitAccumulatedHeal -= 1.0f;
                    Debug.Log($"Healed 1 HP. Current: {stats.ValueRO.health:F1}/{stats.ValueRO.maxHealth:F1}");
                }
            }
            else
            {
                // Stop using med kit
                if (stats.ValueRO.isUsingMedKit)
                {
                    Debug.Log($"Stopped using med kit after {stats.ValueRO.medKitUseTimer:F2}s");
                }
                stats.ValueRW.isUsingMedKit = false;
                stats.ValueRW.medKitUseTimer = 0;
                stats.ValueRW.medKitAccumulatedHeal = 0;
            }
        }
    }
}