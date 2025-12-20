using Unity.Entities;
using UnityEngine;

public class PlayerWeaponAuthoring : MonoBehaviour
{
    public class Baker : Baker<PlayerWeaponAuthoring> {
        public override void Bake(PlayerWeaponAuthoring authoring)
        {
            //Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            //AddComponent(entity, new PlayerWeapon());
        }
    }
}

public struct PlayerWeapon : IComponentData {
    public float firerate;
}