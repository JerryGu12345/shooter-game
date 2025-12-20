using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct GameOverServerSystem : ISystem
{
    private bool gameOverTriggered;

    public void OnCreate(ref SystemState state)
    {
        gameOverTriggered = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        // Only trigger game over once
        if (gameOverTriggered) return;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        EntityManager entityManager = state.EntityManager;

        foreach ((RefRO<Player> player, RefRO<GhostOwner> ghostOwner, Entity playerEntity)
            in SystemAPI.Query<RefRO<Player>, RefRO<GhostOwner>>().WithEntityAccess())
        {
            // Check if player is dead
            if (player.ValueRO.health <= 0)
            {
                Debug.Log($"Player with NetworkId {ghostOwner.ValueRO.NetworkId} died! Triggering game over.");
                gameOverTriggered = true;

                // Send game over RPC to all clients
                foreach ((RefRO<NetworkId> netId, Entity connectionEntity)
                    in SystemAPI.Query<RefRO<NetworkId>>().WithAll<NetworkStreamConnection>().WithEntityAccess())
                {
                    Entity rpcEntity = ecb.CreateEntity();
                    
                    bool isWinner = netId.ValueRO.Value != ghostOwner.ValueRO.NetworkId;
                    
                    Debug.Log($"Sending GameOverRpc to NetworkId {netId.ValueRO.Value}: IsWinner = {isWinner}");
                    
                    ecb.AddComponent(rpcEntity, new GameOverRpc
                    {
                        IsWinner = isWinner
                    });
                    
                    ecb.AddComponent(rpcEntity, new SendRpcCommandRequest
                    {
                        TargetConnection = connectionEntity
                    });
                }

                // Destroy all game entities immediately
                // Destroy all players
                foreach ((RefRO<Player> p, Entity e) in SystemAPI.Query<RefRO<Player>>().WithEntityAccess())
                {
                    ecb.DestroyEntity(e);
                }

                // Destroy all heads
                foreach ((RefRO<PlayerHead> h, Entity e) in SystemAPI.Query<RefRO<PlayerHead>>().WithEntityAccess())
                {
                    ecb.DestroyEntity(e);
                }

                // Destroy all weapons
                foreach ((RefRO<PlayerWeapon> w, Entity e) in SystemAPI.Query<RefRO<PlayerWeapon>>().WithEntityAccess())
                {
                    ecb.DestroyEntity(e);
                }

                // Destroy all bullets
                foreach ((RefRO<Bullet> b, Entity e) in SystemAPI.Query<RefRO<Bullet>>().WithEntityAccess())
                {
                    ecb.DestroyEntity(e);
                }

                Debug.Log("All game entities destroyed!");
                break;
            }
        }

        ecb.Playback(state.EntityManager);
    }
}

public struct GameOverRpc : IRpcCommand
{
    public bool IsWinner;
}