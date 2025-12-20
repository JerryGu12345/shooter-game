using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;

//[BurstCompile]
//[UpdateInGroup(typeof(GhostSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct AttachPlayerChildrenClientSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        EntityManager entityManager = state.EntityManager;

        foreach ((RefRO<AttachPlayerChildrenRpc> rpc, Entity rpcEntity)
            in SystemAPI.Query<RefRO<AttachPlayerChildrenRpc>>().WithEntityAccess())
        {
            
            
            
            //Debug.Log($"[AttachPlayerChildrenClientSystem] Running in {state.World.Name}");
            
            // Find Player, Head, and Weapon Entities
            Entity playerEntity = Entity.Null;
            Entity headEntity = Entity.Null;
            Entity weaponEntity = Entity.Null;

            foreach ((RefRO<GhostInstance> ghost, Entity entity)
                in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess())
            {
                if (ghost.ValueRO.ghostId == rpc.ValueRO.PlayerEntityId){
                    playerEntity = entity;
                    //Debug.Log(ghost.ValueRO.ghostId);
                }
                    
                if (ghost.ValueRO.ghostId == rpc.ValueRO.HeadEntityId){
                    headEntity = entity;
                    //Debug.Log(ghost.ValueRO.ghostId);
                }
                if (ghost.ValueRO.ghostId == rpc.ValueRO.WeaponEntityId){
                    weaponEntity = entity;
                    //Debug.Log(ghost.ValueRO.ghostId);
                }
            }

            
            
            if (playerEntity == Entity.Null || headEntity == Entity.Null || weaponEntity == Entity.Null)
            {
                //Debug.LogError("Failed to attach player children: One or more entities not found!"+" "
                //    +playerEntity.Index+" "+headEntity.Index+" "+weaponEntity.Index);
                continue;
            }
            //Adding this line makes this whole entity hierarchy thing not work for some reason
            //Debug.Log(playerEntity.Index+" "+headEntity.Index+" "+weaponEntity.Index);
            //Removing this line makes other players' entity hierarchy not synced for some reason
            Debug.Log(entityManager.GetComponentData<GhostOwner>(playerEntity).ToSafeString());
            

            // Attach to Player on Client
            entityCommandBuffer.AddComponent(headEntity, new Parent { Value = playerEntity });
            entityCommandBuffer.AddComponent(weaponEntity, new Parent { Value = playerEntity });

            if (!SystemAPI.HasBuffer<Child>(playerEntity))
            {
                entityCommandBuffer.AddBuffer<Child>(playerEntity);
            }

            entityCommandBuffer.AppendToBuffer(playerEntity, new Child { Value = headEntity });
            entityCommandBuffer.AppendToBuffer(playerEntity, new Child { Value = weaponEntity });

            // Destroy the RPC Entity
            entityCommandBuffer.DestroyEntity(rpcEntity);
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }
}
