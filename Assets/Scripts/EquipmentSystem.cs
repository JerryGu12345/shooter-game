using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct EquipmentSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<NetcodePlayerInput> input, RefRW<PlayerEquipment> equipment, Entity entity)
            in SystemAPI.Query<RefRO<NetcodePlayerInput>, RefRW<PlayerEquipment>>().WithAll<Simulate>().WithEntityAccess())
        {
            // Handle swapping timer
            if (equipment.ValueRO.isSwapping)
            {
                equipment.ValueRW.swapTimer -= SystemAPI.Time.DeltaTime;
                if (equipment.ValueRW.swapTimer <= 0)
                {
                    // Complete swap
                    equipment.ValueRW.currentSlot = equipment.ValueRO.pendingSlot;
                    equipment.ValueRW.isSwapping = false;
                    
                    // Update current item size
                    equipment.ValueRW.currentItemSize = GetItemSize(equipment.ValueRO.currentSlot, equipment.ValueRO);
                    
                    Debug.Log($"Swapped to {equipment.ValueRO.currentSlot}");
                }
                continue; // Don't allow swapping while already swapping
            }
            
            // Check for swap inputs
            EquipmentSlot requestedSlot = equipment.ValueRO.currentSlot;
            bool swapRequested = false;
            
            if (input.ValueRO.switchToKnife.IsSet)
            {
                requestedSlot = EquipmentSlot.Knife;
                swapRequested = true;
            }
            else if (input.ValueRO.switchToGun1.IsSet)
            {
                requestedSlot = EquipmentSlot.Gun1;
                swapRequested = true;
            }
            else if (input.ValueRO.switchToGun2.IsSet && equipment.ValueRO.gun2Id != -1)
            {
                requestedSlot = EquipmentSlot.Gun2;
                swapRequested = true;
            }
            else if (input.ValueRO.switchToMedKit.IsSet)
            {
                requestedSlot = EquipmentSlot.MedKit;
                swapRequested = true;
            }
            
            if (swapRequested && requestedSlot != equipment.ValueRO.currentSlot)
            {
                // Start swap
                float oldSize = equipment.ValueRO.currentItemSize;
                float newSize = GetItemSize(requestedSlot, equipment.ValueRO);
                float swapTime = 0.1f * (oldSize + newSize);
                
                equipment.ValueRW.isSwapping = true;
                equipment.ValueRW.swapTimer = swapTime;
                equipment.ValueRW.pendingSlot = requestedSlot;
                
                Debug.Log($"Starting swap from {equipment.ValueRO.currentSlot} to {requestedSlot}, time: {swapTime:F2}s");
            }
        }
    }
    
    private static float GetItemSize(EquipmentSlot slot, PlayerEquipment equipment)
    {
        switch (slot)
        {
            case EquipmentSlot.Knife:
            case EquipmentSlot.MedKit:
                return 1.5f;
            case EquipmentSlot.Gun1:
                // Would need to get gun data from PlayerProgression
                // For now, return a placeholder
                return 2f;
            case EquipmentSlot.Gun2:
                return 2f;
            default:
                return 1.5f;
        }
    }
}