using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct ShootSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        EntityManager entityManager = state.EntityManager;

        foreach (
            (RefRO<NetcodePlayerInput> netcodePlayerInput,
            RefRW<Player> player,
            RefRW<PlayerStats> stats,
            RefRO<PlayerEquipment> equipment,
            RefRO<GhostOwner> ghostOwner,
            Entity entity)
            in SystemAPI.Query<
                RefRO<NetcodePlayerInput>,
                RefRW<Player>,
                RefRW<PlayerStats>,
                RefRO<PlayerEquipment>,
                RefRO<GhostOwner>>().WithAll<Simulate>().WithEntityAccess()) {
            
            // Only shoot if holding a gun and not swapping
            if (equipment.ValueRO.isSwapping ||
                (equipment.ValueRO.currentSlot != EquipmentSlot.Gun1 && equipment.ValueRO.currentSlot != EquipmentSlot.Gun2))
            {
                continue;
            }
            
            // Can't shoot while reloading
            if (stats.ValueRO.isReloading)
            {
                continue;
            }
            
            if (state.World.IsServer()) {
                player.ValueRW.firecooldown -= SystemAPI.Time.DeltaTime;
                if (player.ValueRW.firecooldown > 0f) {
                    continue;
                }
            }

            if (networkTime.IsFirstTimeFullyPredictingTick) {
                if (netcodePlayerInput.ValueRO.shoot.IsSet) {
                    // Check ammo
                    if (stats.ValueRO.currentAmmo <= 0)
                    {
                        // Auto-reload will be triggered by ReloadSystem
                        continue;
                    }
                    
                    foreach (var child in SystemAPI.GetBuffer<Child>(entity))
                    {
                        if (SystemAPI.HasComponent<PlayerWeapon>(child.Value))
                        {
                            var childTransform = SystemAPI.GetComponent<LocalToWorld>(child.Value);
                            Entity bulletEntity = entityCommandBuffer.Instantiate(entitiesReferences.bulletPrefabEntity);
                            entityCommandBuffer.SetComponent(bulletEntity, LocalTransform.FromPositionRotation(
                                childTransform.Position, childTransform.Rotation));

                            PlayerWeapon weapon = SystemAPI.GetComponent<PlayerWeapon>(child.Value);
                            
                            // Use bullet speed from weapon stats (affects visual speed and damage calculation)
                            float bulletMoveSpeed = 10f * weapon.bulletSpeed;
                            
                            entityCommandBuffer.AddComponent(bulletEntity, new Bullet{
                                timer=0.0f,
                                moveSpeed=bulletMoveSpeed,
                                damage=weapon.damage,
                                p0=entityManager.GetComponentData<GhostInstance>(entity).ghostId,
                                p1=-1,
                                p2=-1
                            });

                            entityCommandBuffer.SetComponent(bulletEntity, new GhostOwner{NetworkId = ghostOwner.ValueRO.NetworkId});
                            
                            // Apply fire rate penalty from size
                            float itemSize = equipment.ValueRO.currentItemSize;
                            float adjustedFirerate = weapon.firerate * itemSize;
                            
                            player.ValueRW.firecooldown = adjustedFirerate;
                            
                            // Consume ammo
                            stats.ValueRW.currentAmmo--;
                            
                            Debug.Log($"Fired! Ammo: {stats.ValueRO.currentAmmo}/{stats.ValueRO.maxAmmo}");
                            break;
                        }
                    }
                }
            }
        }
        entityCommandBuffer.Playback(state.EntityManager);
    }
}