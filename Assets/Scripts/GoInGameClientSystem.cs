using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.VisualScripting;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct GoInGameClientSystem : ISystem
{

    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<EntitiesReferences>();
        state.RequireForUpdate<NetworkId>();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach (
            (RefRO<NetworkId> networkId, 
            Entity entity)
            in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess()
        ) {
            
            
            entityCommandBuffer.AddComponent<NetworkStreamInGame>(entity);
            //Debug.Log("Setting client as ingame");

            Entity rpcEntity = entityCommandBuffer.CreateEntity();
            entityCommandBuffer.AddComponent(rpcEntity, new GoInGameRequestRpc());
            entityCommandBuffer.AddComponent(rpcEntity, new SendRpcCommandRequest());
        }
        entityCommandBuffer.Playback(state.EntityManager);
    }
}

public struct GoInGameRequestRpc : IRpcCommand {}