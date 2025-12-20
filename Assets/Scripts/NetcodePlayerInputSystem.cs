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
            /*
            float2 mouseDelta = new float2();
            if (Input.GetKey(KeyCode.UpArrow)) mouseDelta.y = +1f;
            if (Input.GetKey(KeyCode.DownArrow)) mouseDelta.y = -1f;
            if (Input.GetKey(KeyCode.LeftArrow)) mouseDelta.x = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) mouseDelta.x = +1f;
            netcodePlayerInput.ValueRW.mouseDelta = mouseDelta * 0.1f;*/


            
            if (Input.GetKeyDown(KeyCode.Mouse0)) {
                //Debug.Log("shoot");
                netcodePlayerInput.ValueRW.shoot.Set();
            } else {
                netcodePlayerInput.ValueRW.shoot=default;
            }
            
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
