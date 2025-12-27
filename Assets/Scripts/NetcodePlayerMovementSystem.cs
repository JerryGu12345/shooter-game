using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

partial struct NetcodePlayerMovementSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        foreach (
            (RefRO<NetcodePlayerInput> netcodePlayerInput,
            RefRW<LocalTransform> localTransform,
            RefRO<PlayerEquipment> equipment,
            RefRO<PlayerStats> stats,
            RefRW<PhysicsVelocity> velocity,
            Entity entity
            )
            in SystemAPI.Query<
                RefRO<NetcodePlayerInput>,
                RefRW<LocalTransform>,
                RefRO<PlayerEquipment>,
                RefRO<PlayerStats>,
                RefRW<PhysicsVelocity>
            >().WithAll<Simulate,Player>().WithNone<PlayerNeedsChildren>().WithEntityAccess()) {
                
            float baseMoveSpeed = 10f;
            float3 moveVector = netcodePlayerInput.ValueRO.inputVector;
            
            // Apply size penalty
            float itemSize = equipment.ValueRO.currentItemSize;
            float moveSpeed = baseMoveSpeed / itemSize;
            
            // Apply med kit penalty (additional 80% reduction while using)
            if (stats.ValueRO.isUsingMedKit)
            {
                moveSpeed *= 0.2f; // 80% reduction = 20% remaining
            }

            // Move based on player orientation
            moveVector = math.mul(localTransform.ValueRW.Rotation, moveVector);
            
            // Apply jump with size penalty
            if (moveVector.y > 0 && IsGrounded(entity, localTransform.ValueRW.Position, physicsWorldSingleton.CollisionWorld))
            {
                float jumpHeight = 1f / itemSize; // Max 1 meter divided by size
                velocity.ValueRW.Linear.y = math.sqrt(2f * 9.81f * jumpHeight);
                moveVector.y = 0; // Don't apply vertical movement through position
            }
            else
            {
                moveVector.y = 0;
            }
                
            localTransform.ValueRW.Position += moveVector * moveSpeed * SystemAPI.Time.DeltaTime;

            // Mouse movement
            float2 mouseDelta = netcodePlayerInput.ValueRO.mouseDelta;
            float rotateSpeed = 5f;

            // Rotate body (yaw only)
            quaternion bodyRotation = quaternion.AxisAngle(new float3(0, 1, 0), math.radians(mouseDelta.x * rotateSpeed));
            localTransform.ValueRW = localTransform.ValueRW.Rotate(bodyRotation);

            // Check if the player has child entities
            if (SystemAPI.HasBuffer<Child>(entity))
            {
                DynamicBuffer<Child> childEntities = SystemAPI.GetBuffer<Child>(entity);

                foreach (var child in childEntities)
                {
                    if (SystemAPI.HasComponent<PlayerHead>(child.Value) || SystemAPI.HasComponent<PlayerWeapon>(child.Value))
                    {
                        var childTransform = SystemAPI.GetComponent<LocalTransform>(child.Value);

                        // Apply only pitch rotation to head/weapon
                        quaternion headRotation = quaternion.AxisAngle(new float3(1, 0, 0), math.radians(mouseDelta.y * rotateSpeed));
                        childTransform = childTransform.Rotate(headRotation);

                        SystemAPI.SetComponent(child.Value, childTransform);
                    }
                }
            }
        }
    }

    private bool IsGrounded(Entity entity, float3 position, CollisionWorld world)
    {
        var input = new RaycastInput
        {
            Start = position,
            End = position + new float3(0, -0.2f, 0),
            Filter = new CollisionFilter
            {
                BelongsTo = 1u << 0,
                CollidesWith = 1u << 1,
                GroupIndex = 0
            }
        };

        return world.CastRay(input, out var hit);
    }
}