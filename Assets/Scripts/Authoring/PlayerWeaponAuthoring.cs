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
    [GhostField] public float firerate; // Cooldown in seconds
    [GhostField] public int damage;
    [GhostField] public float bulletSpeed;
    [GhostField] public int magSize; // Not used yet, but will be for reloading
}