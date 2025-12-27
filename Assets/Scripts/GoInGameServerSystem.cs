using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Unity.Physics;
using Unity.VisualScripting;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct GoInGameServerSystem : ISystem
{

    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<EntitiesReferences>();
        state.RequireForUpdate<NetworkId>();
    }
    //[BurstCompile]

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        EntityManager entityManager = state.EntityManager;
        foreach (
            (RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest,
            Entity entity)
            in SystemAPI.Query<
                RefRO<ReceiveRpcCommandRequest>
            >().WithAll<GoInGameRequestRpc>().WithEntityAccess())
        {
            
            // Mark the connection as "in game"
            entityCommandBuffer.AddComponent<NetworkStreamInGame>(receiveRpcCommandRequest.ValueRO.SourceConnection);
            Debug.Log("Client connected to server!");

            //Debug.Assert(entitiesReferences.headPrefabEntity != Entity.Null, "Head prefab entity is null!");
            //Debug.Assert(entitiesReferences.weaponPrefabEntity != Entity.Null, "Weapon prefab entity is null!");


            // Spawn player entity
            Entity playerEntity = entityCommandBuffer.Instantiate(entitiesReferences.playerPrefabEntity);
            
            entityCommandBuffer.SetComponent(playerEntity, new Player{
                health=100,
                firecooldown=0f
            });
            entityCommandBuffer.SetComponent(playerEntity, LocalTransform.FromPosition(new float3(
                UnityEngine.Random.Range(-10, 10), 5, 0
            )));

            // Assign the player entity to the client
            NetworkId networkId = SystemAPI.GetComponent<NetworkId>(receiveRpcCommandRequest.ValueRO.SourceConnection);
            entityCommandBuffer.AddComponent(playerEntity, new GhostOwner { NetworkId = networkId.Value });

            //Physics
            entityCommandBuffer.AddComponent(playerEntity, new PhysicsGravityFactor { Value = 1f });
            entityCommandBuffer.AddComponent(playerEntity, new PhysicsVelocity {});
            entityCommandBuffer.AddComponent(playerEntity, new PhysicsMass 
            {
                InverseMass = 1f,
                InverseInertia = float3.zero, // prevents rotation
                Transform = RigidTransform.identity
            });


            // Spawn head entity and attach it to the player
            Entity headEntity = entityCommandBuffer.Instantiate(entitiesReferences.headPrefabEntity);
            entityCommandBuffer.SetComponent(headEntity, LocalTransform.FromPosition(new float3(0, 1.5f, 0))); // Adjust height as needed
            entityCommandBuffer.AddComponent(headEntity, new GhostOwner { NetworkId = networkId.Value });
            //entityCommandBuffer.AddComponent<PlayerHead>(headEntity); // 

            // Spawn weapon entity and attach it to the player
            // In GoInGameServerSystem.cs, after spawning weapon:
            // In GoInGameServerSystem.cs, after spawning weapon:
            Entity weaponEntity = entityCommandBuffer.Instantiate(entitiesReferences.weaponPrefabEntity);
            entityCommandBuffer.SetComponent(weaponEntity, LocalTransform.FromPosition(new float3(0.5f, 0.5f, 0.5f)));
            entityCommandBuffer.AddComponent(weaponEntity, new GhostOwner { NetworkId = networkId.Value });

            // Initialize with default values - ApplyWeaponStatsSystem will set real stats
            entityCommandBuffer.AddComponent(weaponEntity, new PlayerWeapon{
                firerate = 0f,
                damage = 0,
                bulletSpeed = 0f,
                magSize = 0
            });


            // After creating player entity:
            entityCommandBuffer.AddComponent(playerEntity, new PlayerStats{
                health = 0f, // Will be set by InitializePlayerStatsSystem
                maxHealth = 0f,
                currentAmmo = 0,
                maxAmmo = 0,
                reloadTimer = 0f,
                isReloading = false,
                medKitUseTimer = 0f,
                isUsingMedKit = false,
                medKitHealPerSecond = 0f,
                medKitAccumulatedHeal = 0f
            });

            entityCommandBuffer.AddComponent(playerEntity, new PlayerEquipment{
                currentSlot = EquipmentSlot.Gun1,
                gun1Id = -1, // Will be set by InitializePlayerStatsSystem
                gun2Id = -1,
                swapTimer = 0f,
                isSwapping = false,
                pendingSlot = EquipmentSlot.Gun1,
                currentItemSize = 2f
            });

            // Link all entities under the client's connection entity
            entityCommandBuffer.AppendToBuffer(receiveRpcCommandRequest.ValueRO.SourceConnection, new LinkedEntityGroup { Value = playerEntity });
            entityCommandBuffer.AppendToBuffer(receiveRpcCommandRequest.ValueRO.SourceConnection, new LinkedEntityGroup { Value = headEntity });
            entityCommandBuffer.AppendToBuffer(receiveRpcCommandRequest.ValueRO.SourceConnection, new LinkedEntityGroup { Value = weaponEntity });

            // Attach a tag component so another system will set Parent-Child relationship
            entityCommandBuffer.AddComponent(playerEntity, new PlayerNeedsChildren { Head = headEntity, Weapon = weaponEntity });

            

            // Destroy the RPC request entity after processing
            entityCommandBuffer.DestroyEntity(entity);
        }
        entityCommandBuffer.Playback(state.EntityManager);

    }
}


public struct AttachPlayerChildrenRpc : IRpcCommand
{
    public int PlayerEntityId;
    public int HeadEntityId;
    public int WeaponEntityId;
}