using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Physics;
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
            RefRO<GhostOwner> ghostOwner,
            Entity entity)
            in SystemAPI.Query<
                RefRO<NetcodePlayerInput>,
                RefRW<Player>,
                RefRO<GhostOwner>>().WithAll<Simulate>().WithEntityAccess()) {
            
            if (state.World.IsServer()) {
                player.ValueRW.firecooldown -= SystemAPI.Time.DeltaTime;
                if (player.ValueRW.firecooldown > 0f) {
                    continue;
                }
            }

            if (networkTime.IsFirstTimeFullyPredictingTick) {
                if (netcodePlayerInput.ValueRO.shoot.IsSet) {
                    foreach (var child in SystemAPI.GetBuffer<Child>(entity))
                    {
                        if (SystemAPI.HasComponent<PlayerWeapon>(child.Value))
                        {
                            var childTransform = SystemAPI.GetComponent<LocalToWorld>(child.Value);
                            Entity bulletEntity = entityCommandBuffer.Instantiate(entitiesReferences.bulletPrefabEntity);
                            entityCommandBuffer.SetComponent(bulletEntity, LocalTransform.FromPositionRotation(
                                childTransform.Position, childTransform.Rotation));

                            PlayerWeapon weapon = SystemAPI.GetComponent<PlayerWeapon>(child.Value);
                            
                            entityCommandBuffer.AddComponent(bulletEntity, new Bullet{
                                timer=0.0f,
                                moveSpeed=10f,
                                damage=weapon.damage, // Use weapon's damage
                                p0=entityManager.GetComponentData<GhostInstance>(entity).ghostId,
                                p1=-1,
                                p2=-1
                            });

                            entityCommandBuffer.SetComponent(bulletEntity, new GhostOwner{NetworkId = ghostOwner.ValueRO.NetworkId});
                            
                            player.ValueRW.firecooldown=weapon.firerate;
                            break;
                        }
                    }
                }
            }
        }
        entityCommandBuffer.Playback(state.EntityManager);
    }
}