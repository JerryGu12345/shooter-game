using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]

public partial struct AttachPlayerChildrenSystem : ISystem
{
    /*
    public void OnCreate(ref SystemState state)
    {
        // Ensure system only runs when PlayerNeedsChildren exists
        state.RequireForUpdate<PlayerNeedsChildren>();

        // Ensure the GhostCollection singleton exists before running
        state.RequireForUpdate<GhostCollection>();
    }*/
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        EntityManager entityManager = state.EntityManager;

        foreach (
            (RefRO<PlayerNeedsChildren> needsChildren, 
            RefRW<Player> player,
            Entity playerEntity)
            in SystemAPI.Query<RefRO<PlayerNeedsChildren>, RefRW<Player>>().WithEntityAccess())
        {
            //Debug.Log($"[AttachPlayerChildrenSystem] Running in {state.World.Name}");
            Entity headEntity = needsChildren.ValueRO.Head;
            Entity weaponEntity = needsChildren.ValueRO.Weapon;

            int playerEntityId = entityManager.GetComponentData<GhostInstance>(playerEntity).ghostId;
            int headEntityId = entityManager.GetComponentData<GhostInstance>(headEntity).ghostId;
            int weaponEntityId = entityManager.GetComponentData<GhostInstance>(weaponEntity).ghostId;
            if (headEntityId == 0 || weaponEntityId == 0) continue;

            // Attach Head and Weapon to Player
            entityCommandBuffer.AddComponent(headEntity, new Parent { Value = playerEntity });
            entityCommandBuffer.AddComponent(weaponEntity, new Parent { Value = playerEntity });

            // Ensure the player has a Child buffer
            if (!SystemAPI.HasBuffer<Child>(playerEntity))
            {
                entityCommandBuffer.AddBuffer<Child>(playerEntity);
            }

            entityCommandBuffer.AppendToBuffer(playerEntity, new Child { Value = headEntity });
            entityCommandBuffer.AppendToBuffer(playerEntity, new Child { Value = weaponEntity });

            
            // Send RPC to Client
            Entity rpcAttachEntity = entityCommandBuffer.CreateEntity();
            

            //Debug.Log(playerEntityId+" "+headEntityId+" "+weaponEntityId);
            
            entityCommandBuffer.AddComponent(rpcAttachEntity, new AttachPlayerChildrenRpc
            {
                PlayerEntityId = playerEntityId,
                HeadEntityId = headEntityId,
                WeaponEntityId = weaponEntityId
            });

            int networkId = entityManager.GetComponentData<GhostOwner>(playerEntity).NetworkId;
            entityCommandBuffer.AddComponent(rpcAttachEntity, new SendRpcCommandRequest { 
                TargetConnection = GetPlayerConnectionEntity(ref state, networkId, entityManager) 
            });

            //Send to other clients

            foreach (
                (RefRW<Player> oldplayer,
                Entity oldplayerEntity)
                in SystemAPI.Query<RefRW<Player>>().WithEntityAccess().WithNone<PlayerNeedsChildren>())
            {
                
                rpcAttachEntity = entityCommandBuffer.CreateEntity();
                Entity oldheadEntity = needsChildren.ValueRO.Head;
                Entity oldweaponEntity = needsChildren.ValueRO.Weapon;

                DynamicBuffer<Child> childEntities = SystemAPI.GetBuffer<Child>(oldplayerEntity);

                foreach (var child in childEntities)
                {
                    if (SystemAPI.HasComponent<PlayerHead>(child.Value)) {
                        oldheadEntity=child.Value;
                    }
                    if (SystemAPI.HasComponent<PlayerWeapon>(child.Value)) {
                        oldweaponEntity=child.Value;
                    }
                }
                //Debug.Log("new ids:"+playerEntityId+" "+headEntityId+" "+weaponEntityId);

                int oldplayerEntityId = entityManager.GetComponentData<GhostInstance>(oldplayerEntity).ghostId;
                int oldheadEntityId = entityManager.GetComponentData<GhostInstance>(oldheadEntity).ghostId;
                int oldweaponEntityId = entityManager.GetComponentData<GhostInstance>(oldweaponEntity).ghostId;
                //Debug.Log("old ids:"+oldplayerEntityId+" "+oldheadEntityId+" "+oldweaponEntityId);

                entityCommandBuffer.AddComponent(rpcAttachEntity, new AttachPlayerChildrenRpc
                {
                    PlayerEntityId = oldplayerEntityId,
                    HeadEntityId = oldheadEntityId,
                    WeaponEntityId = oldweaponEntityId
                });

                entityCommandBuffer.AddComponent(rpcAttachEntity, new SendRpcCommandRequest { 
                    TargetConnection = GetPlayerConnectionEntity(ref state, networkId, entityManager) 
                });

                // sync new player on existing clients
                rpcAttachEntity = entityCommandBuffer.CreateEntity();
                entityCommandBuffer.AddComponent(rpcAttachEntity, new AttachPlayerChildrenRpc
                {
                    PlayerEntityId = playerEntityId,
                    HeadEntityId = headEntityId,
                    WeaponEntityId = weaponEntityId
                });

                int oldnetworkId = entityManager.GetComponentData<GhostOwner>(oldplayerEntity).NetworkId;
                entityCommandBuffer.AddComponent(rpcAttachEntity, new SendRpcCommandRequest { 
                    TargetConnection = GetPlayerConnectionEntity(ref state, oldnetworkId, entityManager) 
                });

            }


            // Remove the temporary tag
            entityCommandBuffer.RemoveComponent<PlayerNeedsChildren>(playerEntity);
        }

        entityCommandBuffer.Playback(state.EntityManager);

        
    }

    private Entity GetPlayerConnectionEntity(ref SystemState state, int networkId, EntityManager entityManager)
    {
        foreach ((RefRO<NetworkId> netId, Entity connectionEntity)
            in SystemAPI.Query<RefRO<NetworkId>>().WithAll<NetworkStreamConnection>().WithEntityAccess())
        {
            if (netId.ValueRO.Value == networkId)
                return connectionEntity;
        }
        return Entity.Null;
    }
}

