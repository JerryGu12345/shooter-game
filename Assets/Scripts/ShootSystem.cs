using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using System.Collections.Generic;

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
                //Debug.Log("cooldown "+player.ValueRW.firecooldown);
                player.ValueRW.firecooldown -=SystemAPI.Time.DeltaTime;
                if (player.ValueRW.firecooldown>0f) {
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

                            //int networkId = entityManager.GetComponentData<GhostOwner>(entity).NetworkId;
                            entityCommandBuffer.AddComponent(bulletEntity, new Bullet{
                                timer=0.0f,
                                moveSpeed=10f,
                                p0=entityManager.GetComponentData<GhostInstance>(entity).ghostId,
                                p1=-1,
                                p2=-1
                            });
/*
                            entityCommandBuffer.AddComponent(bulletEntity, new PhysicsGravityFactor { Value = 10f });
                            entityCommandBuffer.AddComponent(bulletEntity, new PhysicsVelocity {});
                            entityCommandBuffer.AddComponent(bulletEntity, new PhysicsMass 
                            {
                                InverseMass = 1f,
                                InverseInertia = float3.zero, // prevents rotation
                                Transform = RigidTransform.identity
                            });
                            */
                            entityCommandBuffer.SetComponent(bulletEntity, new GhostOwner{NetworkId = ghostOwner.ValueRO.NetworkId});
                            
                            PlayerWeapon weapon = SystemAPI.GetComponent<PlayerWeapon>(child.Value);
                            player.ValueRW.firecooldown=weapon.firerate;
                            break;
                        }
                    }
                    
/*
                    entityCommandBuffer.AddComponent(bulletEntity, new PhysicsVelocity());
                    var physicsMass = new PhysicsMass
                    {
                        InverseMass = 0.0f, // Normal mass
                        InverseInertia = new float3(0, 1, 0), // Lock rotation on X and Z (Y is free)
                        Transform = RigidTransform.identity
                    };
                    entityCommandBuffer.SetComponent(bulletEntity, physicsMass);            
                    entityCommandBuffer.AddComponent(bulletEntity, new PhysicsGravityFactor { Value = 1f });
                    */
                }
            }
            
        }
        entityCommandBuffer.Playback(state.EntityManager);
    }


}

