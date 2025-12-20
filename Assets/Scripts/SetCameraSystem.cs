using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.XR;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]

public partial struct SetCameraSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        if (!SystemAPI.TryGetSingleton<NetworkId>(out var networkId)) return;

        foreach ((RefRO<GhostOwner> ghostOwner, RefRO<LocalTransform> transform, Entity entity) in 
            SystemAPI.Query<RefRO<GhostOwner>, RefRO<LocalTransform>>().WithAll<Player>().WithEntityAccess())
        {
            //Debug.Log($"[SetCameraSystem] Running in {state.World.Name}");
            if (ghostOwner.ValueRO.NetworkId == networkId.Value)
            {
                foreach (var child in SystemAPI.GetBuffer<Child>(entity))
                {
                    if (SystemAPI.HasComponent<PlayerHead>(child.Value))
                    {
                        var childTransform = SystemAPI.GetComponent<LocalToWorld>(child.Value);
                        mainCamera.transform.position = childTransform.Position;
                        mainCamera.transform.rotation = childTransform.Rotation;
                        break;
                    }
                }

/*
                // Get the player's forward direction
                float3 forwardDirection = math.forward(transform.ValueRO.Rotation);

                // Offset the camera behind the player
                //float3 targetPosition = transform.ValueRO.Position - (forwardDirection * 10) + new float3(0, 5, 0);
                float3 targetPosition = transform.ValueRO.Position;

                // Smoothly move the camera to the target position
                //mainCamera.transform.position = math.lerp(mainCamera.transform.position, targetPosition, 1.0f);
                mainCamera.transform.position = transform.ValueRO.Position;

                // Make the camera look in the direction the player is facing
                //mainCamera.transform.rotation = Quaternion.LookRotation(forwardDirection, math.up());
                mainCamera.transform.rotation = transform.ValueRO.Rotation;
                */
                break;
            }
        }
    }
}