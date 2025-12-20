using UnityEngine;
using Unity.Burst;
using Unity.Entities;

partial struct TestMyValueSystem : ISystem
{

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (
            (RefRO<MyValue> MyValue,
            Entity entity)
            in SystemAPI.Query<RefRO<MyValue>>().WithEntityAccess()) {

            //Debug.Log(MyValue.ValueRO.value+" :: "+entity+" :: "+state.World);

        }
    }

}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct TestMyValueServerSystem : ISystem {
    public void OnUpdate(ref SystemState state) {
        foreach(RefRW<MyValue> myValue
            in SystemAPI.Query<RefRW<MyValue>>()) {

            if (Input.GetKeyDown(KeyCode.Y)) {
                myValue.ValueRW.value = UnityEngine.Random.Range(100,999);
                Debug.Log("Changed "+myValue.ValueRW.value);
            }
        }

        
    }
}
