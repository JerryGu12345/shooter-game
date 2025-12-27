using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
[UpdateInGroup(typeof(GhostInputSystemGroup))]
partial struct NetcodePlayerInputSystem : ISystem
{
[BurstCompile]
public void OnCreate(ref SystemState state)
{
state.RequireForUpdate<NetworkStreamInGame>();
state.RequireForUpdate<NetcodePlayerInput>();
}
//[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    foreach((RefRW<NetcodePlayerInput> netcodePlayerInput,
        RefRW<MyValue> myValue)
        in SystemAPI.Query<RefRW<NetcodePlayerInput>, RefRW<MyValue>>().WithAll<GhostOwnerIsLocal>()) {

        float3 inputVector = new float3();
        if (Input.GetKey(KeyCode.W)) inputVector.z = +1f;
        if (Input.GetKey(KeyCode.S)) inputVector.z = -1f;
        if (Input.GetKey(KeyCode.A)) inputVector.x = -1f;
        if (Input.GetKey(KeyCode.D)) inputVector.x = +1f;
        if (Input.GetKey(KeyCode.Space)) inputVector.y = +10f;
        netcodePlayerInput.ValueRW.inputVector = inputVector;

        netcodePlayerInput.ValueRW.mouseDelta = new float2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        // Shooting
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            netcodePlayerInput.ValueRW.shoot.Set();
        } else {
            netcodePlayerInput.ValueRW.shoot = default;
        }
        
        // Reload
        if (Input.GetKeyDown(KeyCode.R)) {
            netcodePlayerInput.ValueRW.reload.Set();
        } else {
            netcodePlayerInput.ValueRW.reload = default;
        }
        
        // Equipment switching
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            netcodePlayerInput.ValueRW.switchToKnife.Set();
        } else {
            netcodePlayerInput.ValueRW.switchToKnife = default;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            netcodePlayerInput.ValueRW.switchToGun1.Set();
        } else {
            netcodePlayerInput.ValueRW.switchToGun1 = default;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3)) {
            netcodePlayerInput.ValueRW.switchToGun2.Set();
        } else {
            netcodePlayerInput.ValueRW.switchToGun2 = default;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha4)) {
            netcodePlayerInput.ValueRW.switchToMedKit.Set();
        } else {
            netcodePlayerInput.ValueRW.switchToMedKit = default;
        }
        
        // Med kit usage (hold)
        netcodePlayerInput.ValueRW.useMedKit = Input.GetKey(KeyCode.Mouse0);
    }
}

[BurstCompile]
public void OnDestroy(ref SystemState state)
{
    
}
}