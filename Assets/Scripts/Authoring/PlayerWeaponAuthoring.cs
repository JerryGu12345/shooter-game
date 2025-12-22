using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class PlayerWeaponAuthoring : MonoBehaviour
{
    public class Baker : Baker<PlayerWeaponAuthoring> {
        public override void Bake(PlayerWeaponAuthoring authoring)
        {
            // Stats will be applied by ApplyWeaponStatsSystem
        }
    }
}

public struct PlayerWeapon : IComponentData {
    [GhostField] public float firerate;
    [GhostField] public int damage;
}