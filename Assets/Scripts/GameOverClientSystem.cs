using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct GameOverClientSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpc, RefRO<GameOverRpc> gameOverRpc, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GameOverRpc>>().WithEntityAccess())
        {
            Debug.Log($"Client received GameOverRpc: IsWinner = {gameOverRpc.ValueRO.IsWinner}");
            
            // Show UI and schedule return to menu
            if (GameOverUI.Instance != null)
            {
                GameOverUI.Instance.ShowGameOver(gameOverRpc.ValueRO.IsWinner);
            }
            else
            {
                Debug.LogError("GameOverUI.Instance is null! Make sure GameOverUI script is in the scene.");
            }

            // Destroy the RPC entity
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
    }
}