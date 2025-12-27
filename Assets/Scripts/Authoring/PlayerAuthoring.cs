using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
public class PlayerAuthoring : MonoBehaviour
{
public class Baker : Baker<PlayerAuthoring> {
public override void Bake(PlayerAuthoring authoring)
{
Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        // Legacy Player component (kept for compatibility, will phase out)
        AddComponent(entity, new Player());
        
        // New components
        AddComponent(entity, new PlayerStats());
        AddComponent(entity, new PlayerEquipment());
    }
}
}
// Legacy component - keeping for now for compatibility
public struct Player : IComponentData {
[GhostField] public float health;
public float firecooldown;
}
[GhostComponent]
public struct PlayerNeedsChildren : IComponentData
{
public Entity Head;
public Entity Weapon;
}