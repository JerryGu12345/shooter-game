using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct BulletSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        EntityManager entityManager = state.EntityManager;

        foreach (
            (RefRW<LocalTransform> localTransform,
            RefRW<Bullet> bullet,
            Entity entity)
            in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRW<Bullet>>().WithEntityAccess().WithAll<Simulate>()) {
            
            float3 forwardDirection = math.mul(localTransform.ValueRW.Rotation, new float3(0, 0, 1));
            localTransform.ValueRW.Position += bullet.ValueRW.moveSpeed * SystemAPI.Time.DeltaTime * forwardDirection;
            localTransform.ValueRW.Position.y -= bullet.ValueRW.timer * bullet.ValueRW.timer * 0.1f;

            foreach (
                (RefRO<LocalTransform> playerTransform, RefRW<Player> player, Entity playerEntity)
                in SystemAPI.Query<RefRO<LocalTransform>,RefRW<Player>>().WithEntityAccess()) {

                int ghostId = entityManager.GetComponentData<GhostInstance>(playerEntity).ghostId;

                if (bullet.ValueRW.p0==ghostId) {
                    continue;
                }
                
                if (math.distance(localTransform.ValueRW.Position, playerTransform.ValueRO.Position)<1.0f) {
                    if (bullet.ValueRW.p1==-1) {
                        player.ValueRW.health -= bullet.ValueRW.damage; // Use bullet's damage
                        bullet.ValueRW.p1=ghostId;
                        bullet.ValueRW.moveSpeed-=5f;
                        
                        // Award kill coins if player died
                        if (state.World.IsServer() && player.ValueRW.health <= 0)
                        {
                            // Note: Coins will be awarded through GameOverUI
                            Debug.Log($"Player {ghostId} was killed!");
                        }
                        break;
                    } else if (bullet.ValueRW.p1==ghostId) continue;
                    else if (bullet.ValueRW.p2==-1) {
                        player.ValueRW.health -= bullet.ValueRW.damage;
                        bullet.ValueRW.p2=ghostId;
                        bullet.ValueRW.moveSpeed-=5f;
                        
                        if (state.World.IsServer() && player.ValueRW.health <= 0)
                        {
                            Debug.Log($"Player {ghostId} was killed!");
                        }
                        break;
                    } else if (bullet.ValueRW.p2==ghostId) continue;
                    else {
                        player.ValueRW.health -= bullet.ValueRW.damage;
                        bullet.ValueRW.p2=ghostId;
                        bullet.ValueRW.moveSpeed-=5f;
                        
                        if (state.World.IsServer())
                        {
                            if (player.ValueRW.health <= 0)
                            {
                                Debug.Log($"Player {ghostId} was killed!");
                            }
                            entityCommandBuffer.DestroyEntity(entity);
                        }
                        break;
                    }
                }
            }

            if (state.World.IsServer()) {
                bullet.ValueRW.timer += SystemAPI.Time.DeltaTime;
                if (bullet.ValueRW.timer >= 10f || bullet.ValueRW.moveSpeed<0.1f) 
                    entityCommandBuffer.DestroyEntity(entity);
            }
        }
        
        entityCommandBuffer.Playback(state.EntityManager);

        // Debug health display
        foreach ((RefRO<Player> player, Entity entity) in SystemAPI.Query<RefRO<Player>>().WithEntityAccess()) {
            Debug.Log(entity.Index + " " + player.ValueRO.health);
        }
    }
}